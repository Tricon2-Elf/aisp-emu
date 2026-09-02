using System.Text;
using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

namespace aisp.Server.Tests;

public sealed class AdventureHttpEndpointsTests
{
    private static readonly byte[] ScriptText = Encoding.UTF8.GetBytes(
        "#sheetcolor,0xffffffff\r\nPAGEHEADER,name\\Main_001,\r\nCHANGEMAP,map\\ダ・カーポ島\\風見学園,timezone\\day,\r\nADVMSG_SET,msg\\Hi.,name\\name\\Tansy,\r\nPAGEFOOTER\r\n#sheetcolor,0xffffffff\r\nPAGEHEADER,name\\Main_002,\r\nCHANGEMAP,map\\ダ・カーポ島\\風見学園,timezone\\eve,\r\nPAGEFOOTER\r\n"
    );
    private static readonly byte[] DatalistText = Encoding.UTF8.GetBytes(
        "[ACTOR]\r\n0,Tansy,doll,2022031,0,0,\r\n"
    );

    private static (SqliteConnection, DbContextOptions<MainContext>) CreateDb()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<MainContext>().UseSqlite(connection).Options;
        using var ctx = new MainContext(options);
        ctx.Database.EnsureCreated();
        return (connection, options);
    }

    private static HttpContext MultipartRequest(
        Dictionary<string, string> fields,
        params (string Name, byte[] Bytes)[] files
    )
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
        };
        context.Request.Method = "POST";
        context.Request.ContentType = "multipart/form-data; boundary=-00011";
        var formFiles = new FormFileCollection();
        foreach (var (name, bytes) in files)
            formFiles.Add(
                new FormFile(new MemoryStream(bytes), 0, bytes.Length, name, name + "_001")
            );
        context.Request.Form = new FormCollection(
            fields.ToDictionary(kv => kv.Key, kv => new StringValues(kv.Value)),
            formFiles
        );
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ExecuteAsync(IResult result, HttpContext context) =>
        Encoding.UTF8.GetString(await ExecuteBytesAsync(result, context));

    private static async Task<byte[]> ExecuteBytesAsync(IResult result, HttpContext context)
    {
        await result.ExecuteAsync(context);
        return ((MemoryStream)context.Response.Body).ToArray();
    }

    [Fact]
    public async Task Upload_WithTicket_StoresBothParts_AndAnswersOkXml()
    {
        var (connection, options) = CreateDb();
        try
        {
            await using var db = new MainContext(options);
            var user = new User { Username = "author" };
            user.SetPassword("pw");
            db.Users.Add(user);
            await db.SaveChangesAsync();
            await new AdventureWorkRepository(db).RegisterAsync(user.Id, 1, 3, 1);
            var shop = new AdventureShopRepository(db);
            var started = await shop.BeginUploadAsync(
                user.Id,
                1,
                3,
                new AdventureListingDraft("T", "A", 0, "", 100, true, 8)
            );
            Assert.NotNull(started);
            var scriptId = started.Value.Listing.ScriptId;

            var context = MultipartRequest(
                new()
                {
                    ["userid"] = user.Id.ToString(),
                    ["scriptid"] = scriptId.ToString(),
                    ["ticket"] = started.Value.Ticket,
                },
                ("uccadv", ScriptText),
                ("datalist", DatalistText)
            );
            var body = await ExecuteAsync(
                await AdventureHttpEndpointsExtensions.UploadAsync(
                    context.Request,
                    shop,
                    NullLoggerFactory.Instance,
                    TestContext.Current.CancellationToken
                ),
                context
            );

            // The client's parser wants status as a root attribute and text in every child it reads.
            Assert.StartsWith("<?xml", body);
            Assert.Contains("status=\"ok\"", body);
            Assert.Contains("<cms>ok</cms>", body);
            Assert.Contains($"<scriptid>{scriptId}</scriptid>", body);
            Assert.DoesNotContain("<contents></contents>", body);
            var content = await db.AdventureListingContents.SingleAsync(c =>
                c.ScriptId == scriptId
            );
            Assert.Equal(ScriptText, content.Script);
            Assert.Equal(DatalistText, content.Datalist);
            Assert.Equal(
                2,
                await db
                    .AdventureListings.AsNoTracking()
                    .Where(l => l.ScriptId == scriptId)
                    .Select(l => l.Pages)
                    .SingleAsync()
            );

            // The same ticket cannot be replayed.
            var replay = MultipartRequest(
                new()
                {
                    ["userid"] = user.Id.ToString(),
                    ["scriptid"] = scriptId.ToString(),
                    ["ticket"] = started.Value.Ticket,
                },
                ("uccadv", ScriptText)
            );
            var replayBody = await ExecuteAsync(
                await AdventureHttpEndpointsExtensions.UploadAsync(
                    replay.Request,
                    shop,
                    NullLoggerFactory.Instance,
                    TestContext.Current.CancellationToken
                ),
                replay
            );
            Assert.Contains("status=\"fail\"", replayBody);

            // After the report the buyer-side download returns both texts as XML for the client to pack.
            Assert.NotNull(await shop.ConfirmUploadAsync(user.Id, scriptId));
            var ticket = await shop.IssueDownloadTicketAsync(user.Id, scriptId);
            var download = MultipartRequest(
                new()
                {
                    ["userid"] = user.Id.ToString(),
                    ["scriptid"] = scriptId.ToString(),
                    ["ticket"] = ticket!,
                }
            );
            var downloaded = await ExecuteAsync(
                await AdventureHttpEndpointsExtensions.DownloadAsync(
                    download.Request,
                    shop,
                    NullLoggerFactory.Instance,
                    TestContext.Current.CancellationToken
                ),
                download
            );
            Assert.StartsWith("text/xml", download.Response.ContentType);
            Assert.Contains("status=\"ok\"", downloaded);
            Assert.Contains($"<scriptid>{scriptId}</scriptid>", downloaded);
            Assert.Contains(
                "<datalist><![CDATA[[ACTOR]\r\n0,Tansy,doll,2022031,0,0,\r\n]]></datalist>",
                downloaded
            );
            Assert.Contains(
                "<contents><![CDATA[#sheetcolor,0xffffffff\r\nPAGEHEADER,name\\Main_001,",
                downloaded
            );

            // A spent ticket fails in XML too, so the client shows its dialog instead of hanging.
            var again = MultipartRequest(
                new()
                {
                    ["userid"] = user.Id.ToString(),
                    ["scriptid"] = scriptId.ToString(),
                    ["ticket"] = ticket!,
                }
            );
            var refused = await ExecuteAsync(
                await AdventureHttpEndpointsExtensions.DownloadAsync(
                    again.Request,
                    shop,
                    NullLoggerFactory.Instance,
                    TestContext.Current.CancellationToken
                ),
                again
            );
            Assert.Contains("status=\"fail\"", refused);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Upload_WithBadTicket_FailsWithXml()
    {
        var (connection, options) = CreateDb();
        try
        {
            await using var db = new MainContext(options);
            var context = MultipartRequest(
                new()
                {
                    ["userid"] = "1",
                    ["scriptid"] = "1",
                    ["ticket"] = "nope",
                },
                ("uccadv", ScriptText)
            );
            var body = await ExecuteAsync(
                await AdventureHttpEndpointsExtensions.UploadAsync(
                    context.Request,
                    new AdventureShopRepository(db),
                    NullLoggerFactory.Instance,
                    TestContext.Current.CancellationToken
                ),
                context
            );
            Assert.Contains("status=\"fail\"", body);
            Assert.Contains("<error>", body);
            Assert.Contains("<code>2</code>", body);
            Assert.Empty(db.AdventureListingContents);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
