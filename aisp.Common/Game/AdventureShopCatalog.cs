using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Game;

/// <summary>
/// Builds the drama disc shop's wire records from listings: the opening snapshot for the 販売担当 clerk, the
/// lineup pages for genre searches and the purchase-history rows.
/// </summary>
public sealed class AdventureShopCatalog(IAdventureShopRepository shop)
{
    public const int PageSize = AdventureShopGenreSearchRequest.PageSize;

    /// <summary>
    /// The shop's genre tabs and the upload window's ジャンル combo, in the client's order (message ids
    /// 0x640FC400-0x640FC409). The client matches a listing's genre tag against these texts to pick the genre
    /// icon and label. Tab 0, 総合, lists every genre.
    /// </summary>
    public static readonly IReadOnlyList<string> GenreNames =
    [
        "総合",
        "オフィシャル",
        "学園もの",
        "ラブストーリー",
        "ホラー",
        "サスペンス",
        "SF",
        "ミステリー",
        "テスト",
        "その他",
    ];

    public const int AllGenres = 0;

    public static uint ToUnixSeconds(DateTime utc) =>
        (uint)
            Math.Clamp(
                new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToUnixTimeSeconds(),
                0,
                uint.MaxValue
            );

    public static AdventureShopItemRecord ToRecord(AdventureListing listing)
    {
        var genreName =
            listing.Genre >= 0 && listing.Genre < GenreNames.Count ? GenreNames[listing.Genre] : "";
        return new AdventureShopItemRecord
        {
            ScriptId = listing.ScriptId,
            AuthorName = listing.AuthorName,
            Title = listing.Title,
            // The shop only ever sold for デレ: the buy button checks this price against the AI-point purse
            // and sends it back with price type 0. The second price (the ニコニコポイント one) stays 0.
            Price = listing.Price,
            PriceAi = 0,
            Tags = [genreName],
            TagFlags = 1,
            GenreTagIndex = 0,
            Comment = listing.Comment,
            Official = listing.Official ? (byte)1 : (byte)0,
            UploadedAt = ToUnixSeconds(listing.ListedAt ?? listing.CreatedAt),
            Purchases = (uint)Math.Max(0, listing.SalesCount),
            Pages = (uint)Math.Max(0, listing.Pages),
            ContentBytes = Math.Max(0, listing.ContentSize),
        };
    }

    public static AdventureShopHistoryRow ToHistoryRow(AdventurePurchase purchase) =>
        new(ToRecord(purchase.Listing), 0, ToUnixSeconds(purchase.PurchasedAt));

    /// <summary>The first lineup page, the ranking board and the player's purchase history.</summary>
    public async Task<AdventureShopStartedNotify> BuildSnapshotAsync(
        int userId,
        CancellationToken ct = default
    )
    {
        var allCount = await shop.CountListedAsync(ct);
        var (total, page) = await shop.SearchAsync(
            new AdventureShopQuery(null, AdventureShopSort.Newest, 0, PageSize),
            ct
        );
        var ranking = await shop.GetRankingAsync(0, AdventureShopStartedNotify.MaxRankings, ct);
        var history = await shop.GetHistoryAsync(
            userId,
            AdventureShopStartedNotify.MaxHistorys,
            ct
        );
        return new AdventureShopStartedNotify(
            allCount: (ulong)allCount,
            word: "",
            filter: 0,
            sort: AdventureShopSort.Newest,
            index: 0,
            searchCount: (ulong)total,
            items: page.Select(ToRecord).ToList(),
            rankSort: 0,
            rankings: ranking
                .Select(
                    (l, i) =>
                        new AdventureShopRankingRow(
                            ToRecord(l),
                            (ushort)(i + 1),
                            (uint)Math.Max(0, l.SalesCount)
                        )
                )
                .ToList(),
            historys: history.Select(ToHistoryRow).ToList()
        );
    }

    /// <summary>One lineup page for a genre tab / sort / page selection.</summary>
    public async Task<AdventureShopItemNotify> BuildPageAsync(
        AdventureShopGenreSearchRequest request,
        CancellationToken ct = default
    )
    {
        var (total, page) = await shop.SearchAsync(
            new AdventureShopQuery(
                request.Genre == AllGenres ? null : (int)Math.Min(request.Genre, int.MaxValue),
                request.Sort,
                (int)Math.Min(request.Index, int.MaxValue),
                PageSize
            ),
            ct
        );
        return new AdventureShopItemNotify(
            "",
            request.Filter,
            request.Sort,
            request.Index,
            (ulong)total,
            page.Select(ToRecord).ToList()
        );
    }
}
