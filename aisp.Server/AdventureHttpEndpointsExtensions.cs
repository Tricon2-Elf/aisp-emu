using System.Text;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace aisp.Server;

/// <summary>
/// The two plain-HTTP endpoints the client's drama uploader talks to (connection.txt rows 5 and 7:
/// ai-sp/upload.php and ai-sp/download.php). Both are multipart POSTs carrying userid, scriptid and a one-time
/// ticket handed out over the game connection; upload adds the uccadv (packed script) and datalist (actor table)
/// file parts, both plain UTF-8 text. Both answer XML; download carries the two texts back in datalist and
/// contents, and the client itself packs them into the obfuscated UTF-16 cache file dl/drama/ai{scriptid}.txt
/// (the layout <see cref="AdventureScriptPacker"/> reads for imports).
/// </summary>
/// <remarks>
/// The upload reply is parsed by the client at 0x4B0F80 with TinyXML and no NULL checks: the root element (any
/// name) must carry a <c>status</c> attribute; when it is not "fail" and equals "ok" the parser takes the text of
/// the child elements <c>cms</c>, <c>scriptid</c> (atoi) and <c>contents</c>, optionally <c>datalist</c>, and
/// crashes on an element without text. On "fail" it reads <c>error</c>/<c>code</c> and <c>error</c>/<c>description</c>.
/// A nested <c>&lt;cms&gt;</c> wrapper, which is what the field names suggest, killed the client on the first
/// live upload.
/// </remarks>
internal static class AdventureHttpEndpointsExtensions
{
    /// <summary>Well above any real manuscript (the test work is 20 KB) but small enough to keep a bad client from filling the database.</summary>
    private const long MaxPartBytes = 8 * 1024 * 1024;

    internal static WebApplication MapAdventureHttpEndpoints(this WebApplication app)
    {
        app.MapPost("/ai-sp/upload.php", UploadAsync);
        app.MapPost("/ai-sp/download.php", DownloadAsync);
        return app;
    }

    internal static async Task<IResult> UploadAsync(
        HttpRequest request,
        IAdventureShopRepository shop,
        ILoggerFactory loggerFactory,
        CancellationToken ct
    )
    {
        var log = loggerFactory.CreateLogger("AdventureHttp");
        if (!request.HasFormContentType)
        {
            log.LogWarning("upload.php: not multipart ({ContentType})", request.ContentType);
            return Fail(1, "bad request");
        }

        CloseAfterReply(request);
        var form = await request.ReadFormAsync(ct);
        var ticket = form["ticket"].ToString();
        var listing = await shop.RedeemUploadTicketAsync(ticket, ct);
        if (listing is null)
        {
            log.LogWarning(
                "upload.php: rejected ticket for userid {UserId} scriptid {ScriptId}",
                form["userid"].ToString(),
                form["scriptid"].ToString()
            );
            return Fail(2, "invalid ticket");
        }
        if (
            long.TryParse(form["scriptid"], out var claimedScriptId)
            && claimedScriptId != listing.ScriptId
        )
        {
            log.LogWarning(
                "upload.php: scriptid {Claimed} does not match ticket for {ScriptId}",
                claimedScriptId,
                listing.ScriptId
            );
            return Fail(3, "script id mismatch");
        }
        if (int.TryParse(form["userid"], out var claimedUserId) && claimedUserId != listing.UserId)
            log.LogInformation(
                "upload.php: userid field {Claimed} differs from listing owner {UserId}",
                claimedUserId,
                listing.UserId
            );

        var script = await ReadPartAsync(form, "uccadv", ct);
        var datalist = await ReadPartAsync(form, "datalist", ct);
        if (script is null || script.Length == 0)
        {
            log.LogWarning("upload.php: no uccadv part for script {ScriptId}", listing.ScriptId);
            return Fail(4, "missing script");
        }
        if (script.Length > MaxPartBytes || (datalist?.Length ?? 0) > MaxPartBytes)
        {
            log.LogWarning("upload.php: oversized upload for script {ScriptId}", listing.ScriptId);
            return Fail(5, "too large");
        }

        var check = AdventureManuscript.Check(script, datalist);
        if (!check.Ok)
        {
            log.LogWarning(
                "upload.php: refused manuscript for script {ScriptId}: {Error}",
                listing.ScriptId,
                check.Error
            );
            return Fail(6, "invalid manuscript: " + check.Error);
        }

        var stored = await shop.StoreContentAsync(
            listing.ScriptId,
            script,
            datalist ?? [],
            check.Pages,
            ct
        );
        if (stored == AdventureStoreOutcome.TooManyPages)
        {
            log.LogWarning(
                "upload.php: refused manuscript for script {ScriptId}: {Pages} page(s) exceed the work's sheets",
                listing.ScriptId,
                check.Pages
            );
            return Fail(7, "more pages than the work has sheets");
        }
        if (stored != AdventureStoreOutcome.Stored)
            return Fail(2, "invalid ticket");
        log.LogInformation(
            "upload.php: stored script {ScriptId} for user {UserId} work {WorkId}: {ScriptBytes} script bytes, {DatalistBytes} datalist bytes, {Pages} sheet(s) (announced {ContentSize})",
            listing.ScriptId,
            listing.UserId,
            listing.WorkId,
            script.Length,
            datalist?.Length ?? 0,
            check.Pages,
            listing.ContentSize
        );
        return Xml(
            $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<result status=\"ok\">\n<cms>ok</cms>\n<scriptid>{listing.ScriptId}</scriptid>\n<contents>{listing.ScriptId}</contents>\n</result>\n"
        );
    }

    internal static async Task<IResult> DownloadAsync(
        HttpRequest request,
        IAdventureShopRepository shop,
        ILoggerFactory loggerFactory,
        CancellationToken ct
    )
    {
        var log = loggerFactory.CreateLogger("AdventureHttp");
        if (!request.HasFormContentType)
        {
            log.LogWarning("download.php: not multipart ({ContentType})", request.ContentType);
            return Fail(1, "bad request");
        }

        CloseAfterReply(request);
        var form = await request.ReadFormAsync(ct);
        var ticket = form["ticket"].ToString();
        var content = await shop.RedeemDownloadTicketAsync(ticket, ct);
        if (content is null)
        {
            log.LogWarning(
                "download.php: rejected ticket for userid {UserId} scriptid {ScriptId}",
                form["userid"].ToString(),
                form["scriptid"].ToString()
            );
            return Fail(2, "invalid ticket");
        }
        if (
            long.TryParse(form["scriptid"], out var claimedScriptId)
            && claimedScriptId != content.ScriptId
        )
        {
            log.LogWarning(
                "download.php: scriptid {Claimed} does not match ticket for {ScriptId}",
                claimedScriptId,
                content.ScriptId
            );
            return Fail(3, "script id mismatch");
        }

        log.LogInformation(
            "download.php: serving script {ScriptId} ({ScriptBytes} script bytes, {DatalistBytes} datalist bytes)",
            content.ScriptId,
            content.Script.Length,
            content.Datalist.Length
        );
        // The client packs these two texts into its ADV0 cache file itself (routine 0x4B1D10); CDATA keeps the
        // line structure intact through its TinyXML parser.
        return Xml(
            $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<result status=\"ok\">\n<cms>ok</cms>\n<scriptid>{content.ScriptId}</scriptid>\n<datalist><![CDATA[{Cdata(content.Datalist)}]]></datalist>\n<contents><![CDATA[{Cdata(content.Script)}]]></contents>\n</result>\n"
        );
    }

    private static string Cdata(byte[] utf8Text)
    {
        var text = Encoding.UTF8.GetString(AdventureScriptPacker.StripUtf8Bom(utf8Text));
        return text.Replace("]]>", "]]]]><![CDATA[>");
    }

    private static async Task<byte[]?> ReadPartAsync(
        IFormCollection form,
        string name,
        CancellationToken ct
    )
    {
        var file = form.Files.GetFile(name);
        if (file is not null)
        {
            if (file.Length > MaxPartBytes)
                return new byte[MaxPartBytes + 1];
            await using var stream = file.OpenReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);
            return buffer.ToArray();
        }
        // A part without filename= lands in the value collection instead.
        var value = form[name];
        return value.Count == 0 ? null : Encoding.UTF8.GetBytes(value.ToString());
    }

    private static IResult Fail(int code, string description) =>
        Xml(
            $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<result status=\"fail\">\n<error>\n<code>{code}</code>\n<description>{description}</description>\n</error>\n</result>\n"
        );

    private static IResult Xml(string body) => Results.Text(body, "text/xml", Encoding.UTF8);

    /// <summary>
    /// The client's uploader object is recreated per upload but the previous one is only closed, and when that
    /// close completes its destructor clears the window's job pointer without checking it still owns it. With a
    /// kept-alive connection the close lands during the next upload and wipes that upload's job, which then shows
    /// 「原因不明のエラー」 (verified in the client, seen live as every second upload failing). Closing the
    /// connection right after each reply makes the close land while nothing else is stored.
    /// </summary>
    private static void CloseAfterReply(HttpRequest request) =>
        request.HttpContext.Response.Headers.Connection = "close";
}
