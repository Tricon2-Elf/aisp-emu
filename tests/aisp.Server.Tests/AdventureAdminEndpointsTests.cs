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

public sealed class AdventureAdminEndpointsTests
{
    private static readonly byte[] Script = Encoding.UTF8.GetBytes(
        "#sheetcolor,0xffffffff\r\nPAGEHEADER,name\\Main_001,\r\nCHANGEMAP,map\\ダ・カーポ島\\風見学園,timezone\\day,\r\nADVMSG_SET,msg\\Hi.,name\\name\\Tansy,\r\nPAGEFOOTER\r\n"
    );
    private static readonly byte[] Datalist = Encoding.UTF8.GetBytes(
        "[ACTOR]\r\n0,Tansy,doll,2022031,0,0,\r\n"
    );

    private static HttpContext Request(Dictionary<string, string> fields, byte[]? pack)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
        };
        context.Request.Method = "POST";
        context.Request.ContentType = "multipart/form-data; boundary=-00011";
        var files = new FormFileCollection();
        if (pack is not null)
            files.Add(new FormFile(new MemoryStream(pack), 0, pack.Length, "pack", "ai1729.txt"));
        context.Request.Form = new FormCollection(
            fields.ToDictionary(kv => kv.Key, kv => new StringValues(kv.Value)),
            files
        );
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<(int Status, string Body)> RunAsync(
        long scriptId,
        HttpContext context,
        MainContext db
    )
    {
        var result = await AdventureAdminEndpointsExtensions.ImportAsync(
            scriptId,
            context.Request,
            new AdventureShopRepository(db),
            new UserRepository(db),
            TestContext.Current.CancellationToken
        );
        await result.ExecuteAsync(context);
        return (
            context.Response.StatusCode,
            Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray())
        );
    }

    [Fact]
    public async Task Import_UnpacksTheLegacyFile_AndListsItUnderItsId()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        try
        {
            var options = new DbContextOptionsBuilder<MainContext>().UseSqlite(connection).Options;
            await using var db = new MainContext(options);
            db.Database.EnsureCreated();
            var owner = new User { Username = "official" };
            owner.SetPassword("pw");
            db.Users.Add(owner);
            await db.SaveChangesAsync();

            var pack = AdventureScriptPacker.Pack(Script, Datalist);
            var fields = new Dictionary<string, string>
            {
                ["owner"] = "official",
                ["title"] = "コスプレ・メイド・バッティング・ゲーム♪",
                ["author"] = "aisp-emu",
                ["genre"] = "オフィシャル",
                ["comment"] = "Official.",
                ["price"] = "0",
                ["date"] = "1235072734",
            };

            var (status, body) = await RunAsync(1729, Request(fields, pack), db);
            Assert.Equal(200, status);
            Assert.Contains("\"pages\":1", body);

            var listing = await db
                .AdventureListings.Include(l => l.Content)
                .SingleAsync(l => l.ScriptId == 1729);
            Assert.Equal(AdventureListingState.Listed, listing.State);
            Assert.Equal(owner.Id, listing.UserId);
            Assert.Equal(1, listing.Genre);
            // Imports default to readable and official; the PC library's lock and ribbon tab key off these.
            Assert.True(listing.ContentsPublic);
            Assert.True(listing.Official);
            Assert.Equal(new DateTime(2009, 2, 19, 19, 45, 34), listing.ListedAt);
            Assert.Equal(Script, listing.Content!.Script);
            Assert.Equal(Datalist, listing.Content.Datalist);
            // What the shop hands out for it is the same text again.
            Assert.Single(await new AdventureShopRepository(db).GetUploadListAsync(owner.Id));

            // Same id again, an id the server allocates itself, an unknown owner, and a non-pack.
            Assert.Equal(409, (await RunAsync(1729, Request(fields, pack), db)).Status);
            // A content refresh keeps the id, swaps the text and metadata, and re-lists a delisted disc.
            Assert.True(await new AdventureShopRepository(db).DelistAnyAsync(1729));
            var refreshed = new Dictionary<string, string>(fields)
            {
                ["replace"] = "1",
                ["comment"] = "Refreshed.",
            };
            var newPack = AdventureScriptPacker.Pack(
                Script,
                "[ACTOR]\r\n0,Rue,doll,2022031,0,0,\r\n"u8.ToArray()
            );
            var (replaceStatus, replaceBody) = await RunAsync(
                1729,
                Request(refreshed, newPack),
                db
            );
            Assert.Equal(200, replaceStatus);
            Assert.Contains("\"replaced\":true", replaceBody);
            db.ChangeTracker.Clear();
            var replacedListing = await db
                .AdventureListings.Include(l => l.Content)
                .SingleAsync(l => l.ScriptId == 1729);
            Assert.Equal(AdventureListingState.Listed, replacedListing.State);
            Assert.Equal("Refreshed.", replacedListing.Comment);
            Assert.Contains("Rue", Encoding.UTF8.GetString(replacedListing.Content!.Datalist));
            var sealedFields = new Dictionary<string, string>(fields)
            {
                ["public"] = "0",
                ["official"] = "no",
            };
            Assert.Equal(200, (await RunAsync(1936, Request(sealedFields, pack), db)).Status);
            var sealedListing = await db.AdventureListings.SingleAsync(l => l.ScriptId == 1936);
            Assert.False(sealedListing.ContentsPublic);
            Assert.False(sealedListing.Official);
            Assert.Equal(400, (await RunAsync(10001, Request(fields, pack), db)).Status);
            var stranger = new Dictionary<string, string>(fields) { ["owner"] = "nobody" };
            Assert.Equal(404, (await RunAsync(1730, Request(stranger, pack), db)).Status);
            Assert.Equal(400, (await RunAsync(1730, Request(fields, Script), db)).Status);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
