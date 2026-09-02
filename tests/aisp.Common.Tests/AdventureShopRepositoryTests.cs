using aisp.Common.Config;
using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.Tests;

public sealed class AdventureShopRepositoryTests
{
    private static async Task<User> SeedUserAsync(MainContext db, string name, long aiPoints = 0)
    {
        var user = new User
        {
            Username = name,
            AiPoints = aiPoints,
            AdventureSheetStock = 100,
        };
        user.SetPassword("secret");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<AdventureListing> SeedListingAsync(
        MainContext db,
        AdventureShopRepository shop,
        User author,
        int workId,
        long price,
        int genre = 0
    )
    {
        await new AdventureWorkRepository(db).RegisterAsync(author.Id, 1, workId, 1);
        var started = await shop.BeginUploadAsync(
            author.Id,
            1,
            workId,
            new AdventureListingDraft($"Work {workId}", "Author", genre, "", price, true, 100)
        );
        Assert.NotNull(started);
        var scriptId = started.Value.Listing.ScriptId;
        Assert.NotNull(await shop.RedeemUploadTicketAsync(started.Value.Ticket));
        Assert.True(await shop.StoreContentAsync(scriptId, "ADV0"u8.ToArray(), []));
        var listing = await shop.ConfirmUploadAsync(author.Id, scriptId);
        Assert.NotNull(listing);
        return listing;
    }

    [Fact]
    public async Task Buy_DebitsBuyer_RecordsPurchase_AndSettlesIntoAuthorBalance()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var shop = new AdventureShopRepository(db);
            var author = await SeedUserAsync(db, "author");
            var buyer = await SeedUserAsync(db, "buyer", aiPoints: 1500);
            var listing = await SeedListingAsync(db, shop, author, 1, 1000);

            Assert.Equal(
                AdventureBuyOutcome.OwnListing,
                (await shop.BuyAsync(author.Id, 1, listing.ScriptId, 1000, 70)).Outcome
            );
            Assert.Equal(
                AdventureBuyOutcome.PriceMismatch,
                (await shop.BuyAsync(buyer.Id, 2, listing.ScriptId, 999, 70)).Outcome
            );

            var bought = await shop.BuyAsync(buyer.Id, 2, listing.ScriptId, 1000, 70);
            Assert.Equal(AdventureBuyOutcome.Bought, bought.Outcome);
            Assert.Equal(500, bought.AiPoints);
            Assert.NotNull(bought.Purchase);
            Assert.Equal(700, bought.Purchase.AuthorShare);

            Assert.Equal(
                AdventureBuyOutcome.AlreadyOwned,
                (await shop.BuyAsync(buyer.Id, 2, listing.ScriptId, 1000, 70)).Outcome
            );
            Assert.Equal(
                AdventureBuyOutcome.InsufficientFunds,
                (
                    await shop.BuyAsync(
                        buyer.Id,
                        2,
                        (await SeedListingAsync(db, shop, author, 2, 800)).ScriptId,
                        800,
                        70
                    )
                ).Outcome
            );

            var history = await shop.GetHistoryAsync(buyer.Id, 50);
            Assert.Equal(listing.ScriptId, Assert.Single(history).ScriptId);
            Assert.Equal("Work 1", history[0].Listing.Title);

            // Nothing is collectable until the weekly cutoff passes the purchase.
            var before = await shop.GetBalancesAsync(author.Id);
            Assert.Equal(new AdventureSalesBalances(0, 700), before);
            Assert.Equal(0, await shop.SettleAsync(DateTime.UtcNow.AddDays(-1)));
            Assert.Equal(1, await shop.SettleAsync(DateTime.UtcNow.AddMinutes(1)));
            Assert.Equal(0, await shop.SettleAsync(DateTime.UtcNow.AddMinutes(1)));
            Assert.Equal(
                new AdventureSalesBalances(700, 0),
                await shop.GetBalancesAsync(author.Id)
            );

            var paid = await shop.PayoutAsync(author.Id);
            Assert.Equal((700, 700), paid);
            Assert.Equal((0, 700), await shop.PayoutAsync(author.Id));

            // Download tickets go to the buyer and the author only.
            Assert.NotNull(await shop.IssueDownloadTicketAsync(buyer.Id, listing.ScriptId));
            Assert.NotNull(await shop.IssueDownloadTicketAsync(author.Id, listing.ScriptId));
            var stranger = await SeedUserAsync(db, "stranger");
            Assert.Null(await shop.IssueDownloadTicketAsync(stranger.Id, listing.ScriptId));
            var ticket = await shop.IssueDownloadTicketAsync(buyer.Id, listing.ScriptId);
            var content = await shop.RedeemDownloadTicketAsync(ticket!);
            Assert.NotNull(content);
            Assert.Equal("ADV0"u8.ToArray(), content.Script);
            Assert.Null(await shop.RedeemDownloadTicketAsync(ticket!));
            Assert.Equal(
                1,
                await db
                    .AdventureListings.AsNoTracking()
                    .Where(l => l.ScriptId == listing.ScriptId)
                    .Select(l => l.DownloadCount)
                    .SingleAsync()
            );

            // Hiding history keeps the copy.
            Assert.True(await shop.HideHistoryAsync(buyer.Id, listing.ScriptId));
            Assert.Empty(await shop.GetHistoryAsync(buyer.Id, 50));
            Assert.Single(await shop.GetDownloadListAsync(buyer.Id));
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Search_FiltersListedPublishedByGenre_AndSorts()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var shop = new AdventureShopRepository(db);
            var author = await SeedUserAsync(db, "author");
            var a = await SeedListingAsync(db, shop, author, 1, 300, genre: 1);
            var b = await SeedListingAsync(db, shop, author, 2, 100, genre: 2);
            var c = await SeedListingAsync(db, shop, author, 3, 200, genre: 1);
            await shop.DelistAsync(author.Id, c.ScriptId);

            Assert.Equal(2, await shop.CountListedAsync());
            var (total, page) = await shop.SearchAsync(
                new AdventureShopQuery(null, AdventureShopSort.Newest, 0)
            );
            Assert.Equal(2, total);
            Assert.Equal([b.ScriptId, a.ScriptId], page.Select(l => l.ScriptId));

            (total, page) = await shop.SearchAsync(
                new AdventureShopQuery(1, AdventureShopSort.Newest, 0, 50)
            );
            Assert.Equal(1, total);
            Assert.Equal(a.ScriptId, Assert.Single(page).ScriptId);

            // Second page of one, newest first: b then a.
            (_, page) = await shop.SearchAsync(
                new AdventureShopQuery(null, AdventureShopSort.Newest, 1, 1)
            );
            Assert.Equal(a.ScriptId, Assert.Single(page).ScriptId);

            // Most bought puts the disc with a sale first.
            var buyer = await SeedUserAsync(db, "buyer", aiPoints: 1000);
            Assert.Equal(
                AdventureBuyOutcome.Bought,
                (await shop.BuyAsync(buyer.Id, 2, a.ScriptId, 300, 70)).Outcome
            );
            (_, page) = await shop.SearchAsync(
                new AdventureShopQuery(null, AdventureShopSort.MostBought, 0, 50)
            );
            Assert.Equal([a.ScriptId, b.ScriptId], page.Select(l => l.ScriptId));
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public void SettlementOptions_FindSaturdayFiveInTokyo()
    {
        var options = new AdventureSettlementOptions();
        // Wednesday 2026-09-02 12:00 UTC -> last cutoff Saturday 2026-08-29 05:00 JST = 2026-08-28 20:00 UTC.
        var now = new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateTime(2026, 8, 28, 20, 0, 0), options.GetLastCutoffUtc(now));
        Assert.Equal(new DateTime(2026, 9, 4, 20, 0, 0), options.GetNextCutoffUtc(now));
        // Exactly at the cutoff it counts as passed.
        Assert.Equal(
            new DateTime(2026, 9, 4, 20, 0, 0),
            options.GetLastCutoffUtc(new DateTime(2026, 9, 4, 20, 0, 0, DateTimeKind.Utc))
        );
    }
}
