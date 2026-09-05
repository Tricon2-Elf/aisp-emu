using System.Globalization;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace aisp.Server;

/// <summary>
/// API-key routes for managing drama disc listings: importing discs recovered from a legacy download cache
/// (an ai{id}.txt pack plus the metadata the pack does not carry), listing everything, and delisting.
/// </summary>
internal static class AdventureAdminEndpointsExtensions
{
    internal static WebApplication MapAdventureAdminEndpoints(this WebApplication app)
    {
        app.MapGet("/api/adventure/listings", ListAsync);
        app.MapPost("/api/adventure/listings/{scriptId:long}", ImportAsync);
        app.MapDelete("/api/adventure/listings/{scriptId:long}", DelistAsync);
        return app;
    }

    private static async Task<IResult> ListAsync(
        IAdventureShopRepository shop,
        CancellationToken ct
    )
    {
        var listings = await shop.GetAllListingsAsync(ct);
        return Results.Ok(
            new
            {
                listings = listings.Select(l => new
                {
                    l.ScriptId,
                    l.UserId,
                    l.WorkId,
                    l.Title,
                    l.AuthorName,
                    l.Genre,
                    l.Price,
                    l.Pages,
                    l.ContentSize,
                    state = l.State.ToString(),
                    l.ContentsPublic,
                    l.Official,
                    l.SalesCount,
                    l.DownloadCount,
                    l.ListedAt,
                    l.DelistedAt,
                }),
                total = listings.Count,
            }
        );
    }

    /// <summary>
    /// Multipart: file part <c>pack</c> (the ai{id}.txt as the client cached it), fields <c>owner</c> (account
    /// username, required), <c>title</c>, <c>author</c>, <c>genre</c> (0-9 or the Japanese name), <c>comment</c>,
    /// <c>price</c> (デレ), optional <c>date</c> (ISO 8601 or Unix seconds; the 投稿 date shown in the shop),
    /// <c>public</c> (buyers may read the manuscript, the upload dialog's 公開する) and <c>official</c> (the PC
    /// library's 公式配信 tab); both default to 1 for imports and take 0/1, true/false, yes/no. <c>replace</c> (1)
    /// updates an existing listing under that id in place, keeping its purchases and counters.
    /// </summary>
    internal static async Task<IResult> ImportAsync(
        long scriptId,
        HttpRequest request,
        IAdventureShopRepository shop,
        IUserRepository users,
        CancellationToken ct
    )
    {
        if (!request.HasFormContentType)
            return Results.BadRequest(new { error = "multipart form expected" });
        var form = await request.ReadFormAsync(ct);

        var ownerName = form["owner"].ToString();
        if (string.IsNullOrWhiteSpace(ownerName))
            return Results.BadRequest(new { error = "owner is required" });
        var owner = await users.GetByUsernameAsync(ownerName);
        if (owner is null)
            return Results.NotFound(new { error = "owner account not found" });

        var file = form.Files.GetFile("pack");
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { error = "pack file part is required" });
        byte[] blob;
        await using (var stream = file.OpenReadStream())
        using (var buffer = new MemoryStream())
        {
            await stream.CopyToAsync(buffer, ct);
            blob = buffer.ToArray();
        }
        var unpacked = AdventureScriptPacker.Unpack(blob);
        if (unpacked is null)
            return Results.BadRequest(new { error = "pack is not a drama file" });
        var script = AdventureScriptPacker.ToUtf8(unpacked.Value.Script);
        var datalist = AdventureScriptPacker.ToUtf8(unpacked.Value.Datalist);
        var check = AdventureManuscript.Check(script, datalist);
        if (!check.Ok)
            return Results.BadRequest(new { error = "invalid manuscript: " + check.Error });

        var genreField = form["genre"].ToString().Trim();
        int genre;
        if (genreField.Length == 0)
            genre = 0;
        else if (!int.TryParse(genreField, out genre))
        {
            genre = AdventureShopCatalog.GenreNames.ToList().IndexOf(genreField);
            if (genre < 0)
                return Results.BadRequest(new { error = "unknown genre: " + genreField });
        }
        if (genre is < 0 or > 9)
            return Results.BadRequest(new { error = "genre must be 0-9" });

        long price = 0;
        var priceField = form["price"].ToString().Trim();
        if (priceField.Length > 0 && (!long.TryParse(priceField, out price) || price < 0))
            return Results.BadRequest(new { error = "price must be a non-negative integer" });

        DateTime? listedAt = null;
        var dateField = form["date"].ToString().Trim();
        if (dateField.Length > 0)
        {
            if (long.TryParse(dateField, out var unix))
                listedAt = DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
            else if (
                DateTimeOffset.TryParse(
                    dateField,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out var parsed
                )
            )
                listedAt = parsed.UtcDateTime;
            else
                return Results.BadRequest(new { error = "date must be ISO 8601 or Unix seconds" });
        }

        var draft = new AdventureListingDraft(
            Truncate(form["title"].ToString(), 120),
            Truncate(form["author"].ToString(), 36),
            genre,
            Truncate(form["comment"].ToString(), 768),
            price,
            Flag(form["public"], defaultValue: true),
            script.Length + datalist.Length
        );
        if (draft.Title.Length == 0)
            return Results.BadRequest(new { error = "title is required" });

        var outcome = await shop.ImportListingAsync(
            owner.Id,
            scriptId,
            draft,
            script,
            datalist,
            check.Pages,
            listedAt,
            Flag(form["official"], defaultValue: true),
            Flag(form["replace"], defaultValue: false),
            ct
        );
        return outcome switch
        {
            AdventureImportOutcome.Imported or AdventureImportOutcome.Replaced => Results.Ok(
                new
                {
                    scriptId,
                    draft.Title,
                    pages = check.Pages,
                    owner = owner.Username,
                    replaced = outcome == AdventureImportOutcome.Replaced,
                }
            ),
            AdventureImportOutcome.IdReserved => Results.BadRequest(
                new
                {
                    error = $"script ids from {aisp.Common.DAL.Entities.AdventureListing.FirstScriptId} up are handed out by the server; imports must use the legacy id",
                }
            ),
            AdventureImportOutcome.IdTaken => Results.Conflict(
                new { error = "script id already exists" }
            ),
            _ => Results.NotFound(new { error = "owner account not found" }),
        };
    }

    private static async Task<IResult> DelistAsync(
        long scriptId,
        IAdventureShopRepository shop,
        CancellationToken ct
    )
    {
        var delisted = await shop.DelistAnyAsync(scriptId, ct);
        return delisted
            ? Results.Ok(new { scriptId, delisted = true })
            : Results.NotFound(new { error = "no listed disc with that script id" });
    }

    private static bool Flag(StringValues value, bool defaultValue)
    {
        var text = value.ToString().Trim().ToLowerInvariant();
        return text.Length == 0 ? defaultValue : text is "1" or "true" or "yes";
    }

    private static string Truncate(string value, int maxBytes)
    {
        value = value.Trim();
        while (System.Text.Encoding.UTF8.GetByteCount(value) > maxBytes)
            value = value[..^1];
        return value;
    }
}
