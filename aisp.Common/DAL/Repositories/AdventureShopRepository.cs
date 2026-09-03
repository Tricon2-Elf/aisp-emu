using System.Data;
using System.Security.Cryptography;
using aisp.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.DAL.Repositories;

/// <summary>Listing metadata as sent by send_adventure_upload_request.</summary>
public sealed record AdventureListingDraft(
    string Title,
    string AuthorName,
    int Genre,
    string Comment,
    long Price,
    bool ContentsPublic,
    long ContentSize
);

/// <summary>A page request from the shop window.</summary>
/// <param name="Genre">Genre tab; null for every genre.</param>
/// <param name="Sort">Sort mode as sent by the client (see <see cref="AdventureShopSort"/>).</param>
/// <param name="Page">0-based page, as the client's page combo counts.</param>
/// <param name="PageSize">Listings per page; the client accepts at most 50 items per reply and pages by 50.</param>
public sealed record AdventureShopQuery(int? Genre, uint Sort, int Page, int PageSize = 50);

/// <summary>Sort combo of the shop window: 新着順, ダウンロード数が多い順, 購入数が多い順.</summary>
public static class AdventureShopSort
{
    public const uint Newest = 0;
    public const uint MostDownloaded = 1;
    public const uint MostBought = 2;
}

public enum AdventureImportOutcome
{
    Imported = 0,

    /// <summary>The id is in the range the emulator hands out itself (FirstScriptId and up).</summary>
    IdReserved = 1,
    IdTaken = 2,
    UnknownOwner = 3,

    /// <summary>An existing listing was updated in place (metadata and content); purchases and counters kept.</summary>
    Replaced = 4,
}

public enum AdventureBuyOutcome
{
    Bought = 0,
    NotFound = 1,
    NotForSale = 2,
    OwnListing = 3,
    AlreadyOwned = 4,
    PriceMismatch = 5,
    InsufficientFunds = 6,
}

public sealed record AdventureBuyResult(
    AdventureBuyOutcome Outcome,
    long AiPoints,
    AdventurePurchase? Purchase
);

public sealed record AdventureSalesBalances(long Collectable, long Pending);

public interface IAdventureShopRepository
{
    /// <summary>Creates a Pending listing for the work and an upload ticket for it. Null when the user or work is unknown.</summary>
    Task<(AdventureListing Listing, string Ticket)?> BeginUploadAsync(
        int userId,
        int characterId,
        int workId,
        AdventureListingDraft draft,
        CancellationToken ct = default
    );

    /// <summary>Consumes an unexpired upload ticket and returns the Pending listing it was issued for.</summary>
    Task<AdventureListing?> RedeemUploadTicketAsync(string token, CancellationToken ct = default);

    /// <summary>Stores the two uploaded texts; a positive <paramref name="pages"/> (sheets counted in the script) replaces the page count taken from the work.</summary>
    Task<bool> StoreContentAsync(
        long scriptId,
        byte[] script,
        byte[] datalist,
        int pages = 0,
        CancellationToken ct = default
    );

    /// <summary>
    /// Puts a Pending listing that has content on sale, marks the work uploaded and takes down any older listing
    /// of the same work. Null when the listing is not the user's, not pending, or has no content yet.
    /// </summary>
    Task<AdventureListing?> ConfirmUploadAsync(
        int userId,
        long scriptId,
        CancellationToken ct = default
    );

    /// <summary>Marks a Pending listing whose upload the client reported as failed as Abandoned; the id stays consumed.</summary>
    Task<bool> AbandonUploadAsync(int userId, long scriptId, CancellationToken ct = default);

    /// <summary>Takes a listing off sale and lets the work be uploaded again. Buyers keep their copies.</summary>
    Task<bool> DelistAsync(int userId, long scriptId, CancellationToken ct = default);

    Task<IReadOnlyList<AdventureListing>> GetUploadListAsync(
        int userId,
        CancellationToken ct = default
    );

    /// <summary>
    /// Registers a disc recovered from a legacy download cache under its original script id (below
    /// FirstScriptId), already on sale, with the two texts in the stored UTF-8 form. The listing takes the owner's
    /// next work id without a work row, the same state a listing is in once its work was deleted from the
    /// notebook: no slot of the 100 works, no sheets, and the id is never handed out again. With
    /// <paramref name="replace"/> an existing listing under that id is updated in place and put back on sale,
    /// keeping its purchases, counters and work id.
    /// </summary>
    Task<AdventureImportOutcome> ImportListingAsync(
        int ownerUserId,
        long scriptId,
        AdventureListingDraft draft,
        byte[] script,
        byte[] datalist,
        int pages,
        DateTime? listedAtUtc,
        bool official,
        bool replace = false,
        CancellationToken ct = default
    );

    /// <summary>Every listing regardless of owner or state, for administration.</summary>
    Task<IReadOnlyList<AdventureListing>> GetAllListingsAsync(CancellationToken ct = default);

    /// <summary>Takes any listing off sale, whoever owns it. False when it is not Listed.</summary>
    Task<bool> DelistAnyAsync(long scriptId, CancellationToken ct = default);

    Task<long> CountListedAsync(CancellationToken ct = default);

    Task<(long Total, IReadOnlyList<AdventureListing> Page)> SearchAsync(
        AdventureShopQuery query,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<AdventureListing>> GetRankingAsync(
        uint rankSort,
        int take,
        CancellationToken ct = default
    );

    /// <summary>The buyer's most recent 購入履歴 entries, oldest first (the client's list is built by inserting at the front), with listings loaded.</summary>
    Task<IReadOnlyList<AdventurePurchase>> GetHistoryAsync(
        int userId,
        int take,
        CancellationToken ct = default
    );

    /// <summary>Every copy the user holds that was not removed from the download list (own uploads excluded), oldest first, with listings loaded.</summary>
    Task<IReadOnlyList<AdventurePurchase>> GetDownloadListAsync(
        int userId,
        CancellationToken ct = default
    );

    Task<AdventureBuyResult> BuyAsync(
        int userId,
        int characterId,
        long scriptId,
        long offeredPrice,
        int authorRatePercent,
        CancellationToken ct = default
    );

    Task<bool> HideHistoryAsync(int userId, long scriptId, CancellationToken ct = default);

    Task<int> HideAllHistoryAsync(int userId, CancellationToken ct = default);

    Task<bool> HideDownloadAsync(int userId, long scriptId, CancellationToken ct = default);

    /// <summary>Issues a download ticket when the user bought the listing or wrote it. Null otherwise.</summary>
    Task<string?> IssueDownloadTicketAsync(
        int userId,
        long scriptId,
        CancellationToken ct = default
    );

    /// <summary>Consumes an unexpired download ticket and returns the listing content.</summary>
    Task<AdventureListingContent?> RedeemDownloadTicketAsync(
        string token,
        CancellationToken ct = default
    );

    /// <summary>Moves the author share of every unsettled purchase made before the cutoff into the authors' balances. Returns the number of purchases settled.</summary>
    Task<int> SettleAsync(DateTime cutoffUtc, CancellationToken ct = default);

    Task<AdventureSalesBalances?> GetBalancesAsync(int userId, CancellationToken ct = default);

    /// <summary>Pays the collectable balance into the user's デレ purse (the in-game currency, User.AiPoints). Null when the user is unknown.</summary>
    Task<(long Paid, long AiPoints)?> PayoutAsync(int userId, CancellationToken ct = default);
}

public sealed class AdventureShopRepository(MainContext db) : IAdventureShopRepository
{
    /// <summary>The client copies the ticket into a 40-byte form field.</summary>
    public const int TicketLength = 40;
    public static readonly TimeSpan TicketLifetime = TimeSpan.FromMinutes(15);

    /// <summary>How long a purchase blocks buying the same disc again (the client's own rule for its 購入履歴).</summary>
    public static readonly TimeSpan RebuyInterval = TimeSpan.FromDays(7);

    private const string TicketAlphabet =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string NewTicket() =>
        RandomNumberGenerator.GetString(TicketAlphabet, TicketLength);

    public async Task<(AdventureListing Listing, string Ticket)?> BeginUploadAsync(
        int userId,
        int characterId,
        int workId,
        AdventureListingDraft draft,
        CancellationToken ct = default
    )
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );
        var work = await db
            .AdventureWorks.AsNoTracking()
            .SingleOrDefaultAsync(w => w.UserId == userId && w.WorkId == workId, ct);
        if (work is null)
            return null;

        // A retried 同意する supersedes the previous attempt; only one pending upload per work at a time.
        var stale = await db
            .AdventureListings.Where(l =>
                l.UserId == userId && l.WorkId == workId && l.State == AdventureListingState.Pending
            )
            .ToListAsync(ct);
        if (stale.Count > 0)
        {
            var staleIds = stale.Select(l => l.ScriptId).ToList();
            await db
                .AdventureTickets.Where(t => staleIds.Contains(t.ScriptId))
                .ExecuteDeleteAsync(ct);
            foreach (var old in stale)
                old.State = AdventureListingState.Abandoned;
            await db.SaveChangesAsync(ct);
        }

        var now = DateTime.UtcNow;
        var lastScriptId = await db.AdventureListings.MaxAsync(l => (long?)l.ScriptId, ct) ?? 0;
        var listing = new AdventureListing
        {
            ScriptId = Math.Max(AdventureListing.FirstScriptId, lastScriptId + 1),
            UserId = userId,
            CharacterId = characterId,
            WorkId = workId,
            Title = draft.Title,
            AuthorName = draft.AuthorName,
            Genre = draft.Genre,
            Comment = draft.Comment,
            Price = Math.Max(0, draft.Price),
            ContentsPublic = draft.ContentsPublic,
            ContentSize = Math.Max(0, draft.ContentSize),
            Pages = work.Sheets,
            State = AdventureListingState.Pending,
            CreatedAt = now,
        };
        db.AdventureListings.Add(listing);
        await db.SaveChangesAsync(ct);

        var ticket = await IssueTicketAsync(
            userId,
            listing.ScriptId,
            AdventureTicketPurpose.Upload,
            now,
            ct
        );
        await transaction.CommitAsync(ct);
        return (listing, ticket);
    }

    private async Task<string> IssueTicketAsync(
        int userId,
        long scriptId,
        AdventureTicketPurpose purpose,
        DateTime now,
        CancellationToken ct
    )
    {
        await db
            .AdventureTickets.Where(t => t.ExpiresAt < now || t.ConsumedAt != null)
            .ExecuteDeleteAsync(ct);
        var token = NewTicket();
        db.AdventureTickets.Add(
            new AdventureTicket
            {
                Token = token,
                UserId = userId,
                ScriptId = scriptId,
                Purpose = purpose,
                CreatedAt = now,
                ExpiresAt = now + TicketLifetime,
            }
        );
        await db.SaveChangesAsync(ct);
        return token;
    }

    private async Task<AdventureTicket?> RedeemTicketAsync(
        string token,
        AdventureTicketPurpose purpose,
        CancellationToken ct
    )
    {
        if (string.IsNullOrEmpty(token) || token.Length > TicketLength)
            return null;
        var now = DateTime.UtcNow;
        var ticket = await db.AdventureTickets.SingleOrDefaultAsync(t => t.Token == token, ct);
        if (
            ticket is null
            || ticket.Purpose != purpose
            || ticket.ConsumedAt != null
            || ticket.ExpiresAt < now
        )
            return null;
        ticket.ConsumedAt = now;
        await db.SaveChangesAsync(ct);
        return ticket;
    }

    public async Task<AdventureListing?> RedeemUploadTicketAsync(
        string token,
        CancellationToken ct = default
    )
    {
        var ticket = await RedeemTicketAsync(token, AdventureTicketPurpose.Upload, ct);
        if (ticket is null)
            return null;
        return await db.AdventureListings.SingleOrDefaultAsync(
            l =>
                l.ScriptId == ticket.ScriptId
                && l.UserId == ticket.UserId
                && l.State == AdventureListingState.Pending,
            ct
        );
    }

    public async Task<bool> StoreContentAsync(
        long scriptId,
        byte[] script,
        byte[] datalist,
        int pages = 0,
        CancellationToken ct = default
    )
    {
        var listing = await db
            .AdventureListings.Include(l => l.Content)
            .SingleOrDefaultAsync(l => l.ScriptId == scriptId, ct);
        if (listing is null)
            return false;
        if (pages > 0)
            listing.Pages = pages;
        if (listing.Content is null)
            listing.Content = new AdventureListingContent { ScriptId = scriptId };
        listing.Content.Script = script;
        listing.Content.Datalist = datalist;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<AdventureListing?> ConfirmUploadAsync(
        int userId,
        long scriptId,
        CancellationToken ct = default
    )
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );
        var listing = await db.AdventureListings.SingleOrDefaultAsync(
            l => l.ScriptId == scriptId && l.UserId == userId,
            ct
        );
        if (listing is null || listing.State != AdventureListingState.Pending)
            return null;
        var hasContent = await db.AdventureListingContents.AnyAsync(
            c => c.ScriptId == scriptId,
            ct
        );
        if (!hasContent)
            return null;

        var now = DateTime.UtcNow;
        var previous = await db
            .AdventureListings.Where(l =>
                l.UserId == userId
                && l.WorkId == listing.WorkId
                && l.ScriptId != scriptId
                && l.State == AdventureListingState.Listed
            )
            .ToListAsync(ct);
        foreach (var old in previous)
        {
            old.State = AdventureListingState.Delisted;
            old.DelistedAt = now;
        }
        listing.State = AdventureListingState.Listed;
        listing.ListedAt = now;

        var work = await db.AdventureWorks.SingleOrDefaultAsync(
            w => w.UserId == userId && w.WorkId == listing.WorkId,
            ct
        );
        if (work is not null)
            work.Uploaded = true;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return listing;
    }

    public async Task<bool> AbandonUploadAsync(
        int userId,
        long scriptId,
        CancellationToken ct = default
    )
    {
        var abandoned = await db
            .AdventureListings.Where(l =>
                l.ScriptId == scriptId
                && l.UserId == userId
                && l.State == AdventureListingState.Pending
            )
            .ExecuteUpdateAsync(
                s => s.SetProperty(l => l.State, AdventureListingState.Abandoned),
                ct
            );
        return abandoned > 0;
    }

    public async Task<bool> DelistAsync(int userId, long scriptId, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );
        var listing = await db.AdventureListings.SingleOrDefaultAsync(
            l => l.ScriptId == scriptId && l.UserId == userId,
            ct
        );
        if (listing is null || listing.State != AdventureListingState.Listed)
            return false;
        listing.State = AdventureListingState.Delisted;
        listing.DelistedAt = DateTime.UtcNow;
        var stillListed = await db.AdventureListings.AnyAsync(
            l =>
                l.UserId == userId
                && l.WorkId == listing.WorkId
                && l.ScriptId != scriptId
                && l.State == AdventureListingState.Listed,
            ct
        );
        if (!stillListed)
        {
            var work = await db.AdventureWorks.SingleOrDefaultAsync(
                w => w.UserId == userId && w.WorkId == listing.WorkId,
                ct
            );
            if (work is not null)
                work.Uploaded = false;
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<AdventureListing>> GetUploadListAsync(
        int userId,
        CancellationToken ct = default
    ) =>
        await db
            .AdventureListings.Where(l =>
                l.UserId == userId && l.State == AdventureListingState.Listed
            )
            .OrderBy(l => l.ScriptId)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<AdventureImportOutcome> ImportListingAsync(
        int ownerUserId,
        long scriptId,
        AdventureListingDraft draft,
        byte[] script,
        byte[] datalist,
        int pages,
        DateTime? listedAtUtc,
        bool official,
        bool replace = false,
        CancellationToken ct = default
    )
    {
        if (scriptId <= 0 || scriptId >= AdventureListing.FirstScriptId)
            return AdventureImportOutcome.IdReserved;
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );
        if (!await db.Users.AnyAsync(u => u.Id == ownerUserId, ct))
            return AdventureImportOutcome.UnknownOwner;
        var now = DateTime.UtcNow;
        var existing = await db
            .AdventureListings.Include(l => l.Content)
            .SingleOrDefaultAsync(l => l.ScriptId == scriptId, ct);
        if (existing is not null)
        {
            if (!replace)
                return AdventureImportOutcome.IdTaken;
            if (existing.WorkId <= 0 || existing.UserId != ownerUserId)
                existing.WorkId = await ConsumeWorkIdAsync(ownerUserId, ct);
            existing.UserId = ownerUserId;
            existing.Title = draft.Title;
            existing.AuthorName = draft.AuthorName;
            existing.Genre = draft.Genre;
            existing.Comment = draft.Comment;
            existing.Price = Math.Max(0, draft.Price);
            existing.ContentsPublic = draft.ContentsPublic;
            existing.Official = official;
            existing.ContentSize = Math.Max(0, draft.ContentSize);
            existing.Pages = Math.Max(0, pages);
            existing.State = AdventureListingState.Listed;
            existing.DelistedAt = null;
            if (listedAtUtc is not null)
                existing.ListedAt = listedAtUtc;
            existing.Content ??= new AdventureListingContent { ScriptId = scriptId };
            existing.Content.Script = script;
            existing.Content.Datalist = datalist;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return AdventureImportOutcome.Replaced;
        }
        var listedAt = listedAtUtc ?? now;
        var workId = await ConsumeWorkIdAsync(ownerUserId, ct);
        db.AdventureListings.Add(
            new AdventureListing
            {
                ScriptId = scriptId,
                UserId = ownerUserId,
                CharacterId = 0,
                WorkId = workId,
                Title = draft.Title,
                AuthorName = draft.AuthorName,
                Genre = draft.Genre,
                Comment = draft.Comment,
                Price = Math.Max(0, draft.Price),
                ContentsPublic = draft.ContentsPublic,
                Official = official,
                ContentSize = Math.Max(0, draft.ContentSize),
                Pages = Math.Max(0, pages),
                State = AdventureListingState.Listed,
                CreatedAt = listedAt,
                ListedAt = listedAt,
                Content = new AdventureListingContent
                {
                    ScriptId = scriptId,
                    Script = script,
                    Datalist = datalist,
                },
            }
        );
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return AdventureImportOutcome.Imported;
    }

    /// <summary>Takes the owner's next work id for a listing without creating a work row, so 新規作成 can never reuse it.</summary>
    private async Task<int> ConsumeWorkIdAsync(int ownerUserId, CancellationToken ct)
    {
        var user = await db.Users.SingleAsync(u => u.Id == ownerUserId, ct);
        var workId = user.NextAdventureWorkId;
        user.NextAdventureWorkId = Math.Min(workId + 1, ushort.MaxValue);
        await db.SaveChangesAsync(ct);
        return workId;
    }

    public async Task<IReadOnlyList<AdventureListing>> GetAllListingsAsync(
        CancellationToken ct = default
    ) => await db.AdventureListings.OrderBy(l => l.ScriptId).AsNoTracking().ToListAsync(ct);

    public async Task<bool> DelistAnyAsync(long scriptId, CancellationToken ct = default)
    {
        var listing = await db.AdventureListings.SingleOrDefaultAsync(
            l => l.ScriptId == scriptId,
            ct
        );
        if (listing is null)
            return false;
        return await DelistAsync(listing.UserId, scriptId, ct);
    }

    // ContentsPublic only decides whether buyers may read the manuscript; sealed discs are still for sale.
    private IQueryable<AdventureListing> Listed() =>
        db.AdventureListings.Where(l => l.State == AdventureListingState.Listed);

    public async Task<long> CountListedAsync(CancellationToken ct = default) =>
        await Listed().LongCountAsync(ct);

    public async Task<(long Total, IReadOnlyList<AdventureListing> Page)> SearchAsync(
        AdventureShopQuery query,
        CancellationToken ct = default
    )
    {
        var listings = Listed();
        if (query.Genre is { } genre)
            listings = listings.Where(l => l.Genre == genre);
        var total = await listings.LongCountAsync(ct);
        IOrderedQueryable<AdventureListing> ordered = query.Sort switch
        {
            AdventureShopSort.MostDownloaded => listings
                .OrderByDescending(l => l.DownloadCount)
                .ThenByDescending(l => l.ScriptId),
            AdventureShopSort.MostBought => listings
                .OrderByDescending(l => l.SalesCount)
                .ThenByDescending(l => l.ScriptId),
            _ => listings.OrderByDescending(l => l.ScriptId),
        };
        var pageSize = Math.Clamp(query.PageSize, 0, 50);
        var page = await ordered
            .Skip(Math.Max(0, query.Page) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);
        return (total, page);
    }

    public async Task<IReadOnlyList<AdventureListing>> GetRankingAsync(
        uint rankSort,
        int take,
        CancellationToken ct = default
    )
    {
        var listings = Listed();
        IOrderedQueryable<AdventureListing> ordered = rankSort switch
        {
            1 => listings.OrderByDescending(l => l.DownloadCount).ThenByDescending(l => l.ScriptId),
            _ => listings.OrderByDescending(l => l.SalesCount).ThenByDescending(l => l.ScriptId),
        };
        return await ordered.Take(Math.Clamp(take, 0, 5)).AsNoTracking().ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AdventurePurchase>> GetHistoryAsync(
        int userId,
        int take,
        CancellationToken ct = default
    )
    {
        var newest = await db
            .AdventurePurchases.Where(p => p.BuyerUserId == userId && !p.HiddenFromHistory)
            .Include(p => p.Listing)
            .OrderByDescending(p => p.Id)
            .Take(Math.Clamp(take, 0, 50))
            .AsNoTracking()
            .ToListAsync(ct);
        newest.Reverse();
        return newest;
    }

    public async Task<IReadOnlyList<AdventurePurchase>> GetDownloadListAsync(
        int userId,
        CancellationToken ct = default
    ) =>
        await db
            .AdventurePurchases.Where(p => p.BuyerUserId == userId && !p.HiddenFromDownloads)
            .Include(p => p.Listing)
            .OrderBy(p => p.Id)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<AdventureBuyResult> BuyAsync(
        int userId,
        int characterId,
        long scriptId,
        long offeredPrice,
        int authorRatePercent,
        CancellationToken ct = default
    )
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return new AdventureBuyResult(AdventureBuyOutcome.NotFound, 0, null);
        var listing = await db.AdventureListings.SingleOrDefaultAsync(
            l => l.ScriptId == scriptId,
            ct
        );
        if (listing is null)
            return new AdventureBuyResult(AdventureBuyOutcome.NotFound, user.AiPoints, null);
        if (listing.State != AdventureListingState.Listed)
            return new AdventureBuyResult(AdventureBuyOutcome.NotForSale, user.AiPoints, null);
        if (listing.UserId == userId)
            return new AdventureBuyResult(AdventureBuyOutcome.OwnListing, user.AiPoints, null);
        // The client itself refuses a second purchase while a history entry is younger than 7 days, and lets
        // the player buy the disc again after that (the original's re-download window).
        var recentCutoff = DateTime.UtcNow - RebuyInterval;
        if (
            await db.AdventurePurchases.AnyAsync(
                p =>
                    p.BuyerUserId == userId
                    && p.ScriptId == scriptId
                    && p.PurchasedAt > recentCutoff,
                ct
            )
        )
            return new AdventureBuyResult(AdventureBuyOutcome.AlreadyOwned, user.AiPoints, null);
        if (offeredPrice != listing.Price)
            return new AdventureBuyResult(AdventureBuyOutcome.PriceMismatch, user.AiPoints, null);
        if (user.AiPoints < listing.Price)
            return new AdventureBuyResult(
                AdventureBuyOutcome.InsufficientFunds,
                user.AiPoints,
                null
            );

        var rate = Math.Clamp(authorRatePercent, 0, 100);
        var purchase = new AdventurePurchase
        {
            ScriptId = scriptId,
            BuyerUserId = userId,
            BuyerCharacterId = characterId,
            Price = listing.Price,
            AuthorShare = listing.Price * rate / 100,
            PurchasedAt = DateTime.UtcNow,
        };
        user.AiPoints -= listing.Price;
        listing.SalesCount++;
        db.AdventurePurchases.Add(purchase);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        purchase.Listing = listing;
        return new AdventureBuyResult(AdventureBuyOutcome.Bought, user.AiPoints, purchase);
    }

    public async Task<bool> HideHistoryAsync(
        int userId,
        long scriptId,
        CancellationToken ct = default
    )
    {
        var changed = await db
            .AdventurePurchases.Where(p =>
                p.BuyerUserId == userId && p.ScriptId == scriptId && !p.HiddenFromHistory
            )
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.HiddenFromHistory, true), ct);
        return changed > 0;
    }

    public async Task<int> HideAllHistoryAsync(int userId, CancellationToken ct = default) =>
        await db
            .AdventurePurchases.Where(p => p.BuyerUserId == userId && !p.HiddenFromHistory)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.HiddenFromHistory, true), ct);

    public async Task<bool> HideDownloadAsync(
        int userId,
        long scriptId,
        CancellationToken ct = default
    )
    {
        var changed = await db
            .AdventurePurchases.Where(p =>
                p.BuyerUserId == userId && p.ScriptId == scriptId && !p.HiddenFromDownloads
            )
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.HiddenFromDownloads, true), ct);
        return changed > 0;
    }

    public async Task<string?> IssueDownloadTicketAsync(
        int userId,
        long scriptId,
        CancellationToken ct = default
    )
    {
        var listing = await db
            .AdventureListings.AsNoTracking()
            .SingleOrDefaultAsync(l => l.ScriptId == scriptId, ct);
        if (listing is null || listing.State == AdventureListingState.Pending)
            return null;
        var entitled =
            listing.UserId == userId
            || await db.AdventurePurchases.AnyAsync(
                p => p.BuyerUserId == userId && p.ScriptId == scriptId,
                ct
            );
        if (!entitled)
            return null;
        return await IssueTicketAsync(
            userId,
            scriptId,
            AdventureTicketPurpose.Download,
            DateTime.UtcNow,
            ct
        );
    }

    public async Task<AdventureListingContent?> RedeemDownloadTicketAsync(
        string token,
        CancellationToken ct = default
    )
    {
        var ticket = await RedeemTicketAsync(token, AdventureTicketPurpose.Download, ct);
        if (ticket is null)
            return null;
        var content = await db
            .AdventureListingContents.AsNoTracking()
            .SingleOrDefaultAsync(c => c.ScriptId == ticket.ScriptId, ct);
        if (content is null)
            return null;
        await db
            .AdventureListings.Where(l => l.ScriptId == ticket.ScriptId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(l => l.DownloadCount, l => l.DownloadCount + 1),
                ct
            );
        // A re-download from the 購入履歴 puts a disc removed from the PC library back on the download list.
        await db
            .AdventurePurchases.Where(p =>
                p.BuyerUserId == ticket.UserId
                && p.ScriptId == ticket.ScriptId
                && p.HiddenFromDownloads
            )
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.HiddenFromDownloads, false), ct);
        return content;
    }

    public async Task<int> SettleAsync(DateTime cutoffUtc, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );
        var due = await db
            .AdventurePurchases.Where(p => p.SettledAt == null && p.PurchasedAt < cutoffUtc)
            .Include(p => p.Listing)
            .ToListAsync(ct);
        if (due.Count == 0)
            return 0;
        var now = DateTime.UtcNow;
        var byAuthor = due.GroupBy(p => p.Listing.UserId)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.AuthorShare));
        var authorIds = byAuthor.Keys.ToList();
        var authors = await db.Users.Where(u => authorIds.Contains(u.Id)).ToListAsync(ct);
        foreach (var author in authors)
            author.AdventureSalesBalance = checked(
                author.AdventureSalesBalance + byAuthor[author.Id]
            );
        foreach (var purchase in due)
            purchase.SettledAt = now;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return due.Count;
    }

    public async Task<AdventureSalesBalances?> GetBalancesAsync(
        int userId,
        CancellationToken ct = default
    )
    {
        var collectable = await db
            .Users.Where(u => u.Id == userId)
            .Select(u => (long?)u.AdventureSalesBalance)
            .SingleOrDefaultAsync(ct);
        if (collectable is null)
            return null;
        var pending = await db
            .AdventurePurchases.Where(p => p.SettledAt == null && p.Listing.UserId == userId)
            .SumAsync(p => p.AuthorShare, ct);
        return new AdventureSalesBalances(collectable.Value, pending);
    }

    public async Task<(long Paid, long AiPoints)?> PayoutAsync(
        int userId,
        CancellationToken ct = default
    )
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return null;
        var paid = user.AdventureSalesBalance;
        if (paid > 0)
        {
            user.AiPoints = checked(user.AiPoints + paid);
            user.AdventureSalesBalance = 0;
            await db.SaveChangesAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return (paid, user.AiPoints);
    }
}
