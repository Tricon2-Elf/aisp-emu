using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Tests.Support;

namespace aisp.Common.Tests;

public sealed class ReportTicketRepositoryTests
{
    [Fact]
    public async Task CreateAsync_PersistsTicketWithSnapshots()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;

        ReportTicket created;
        await using (var db = new MainContext(options))
        {
            var repo = new ReportTicketRepository(db);
            created = await repo.CreateAsync(
                new ReportTicketCreateRequest(
                    1,
                    "reporter",
                    100,
                    "ReporterChar",
                    "Bob is being racist",
                    10990100,
                    1,
                    "Akihabara",
                    [
                        new ReportTicketPlayerSnapshot(1, "reporter", 100, "ReporterChar"),
                        new ReportTicketPlayerSnapshot(2, "other", 200, "OtherChar"),
                    ],
                    [
                        new ReportTicketChatSnapshot(
                            new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                            200,
                            "OtherChar",
                            "bad message",
                            true
                        ),
                    ]
                ),
                TestContext.Current.CancellationToken
            );
        }

        await using (var db = new MainContext(options))
        {
            var repo = new ReportTicketRepository(db);
            var loaded = await repo.GetByIdAsync(
                created.Id,
                TestContext.Current.CancellationToken
            );
            Assert.NotNull(loaded);
            Assert.Equal(ReportTicketStatus.Open, loaded.Status);
            Assert.Equal("Bob is being racist", loaded.Reason);
            Assert.Equal("Akihabara", loaded.MapName);
            Assert.Equal(2, loaded.Players.Count);
            Assert.Single(loaded.ChatMessages);
            Assert.True(loaded.ChatMessages.First().Rejected);
        }
    }

    [Fact]
    public async Task ListAsync_FiltersByStatusAndReturnsNewestFirst()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        await using var db = new MainContext(options);
        var repo = new ReportTicketRepository(db);

        var older = await repo.CreateAsync(
            CreateRequest("older"),
            TestContext.Current.CancellationToken
        );
        var newer = await repo.CreateAsync(
            CreateRequest("newer"),
            TestContext.Current.CancellationToken
        );
        await repo.ResolveAsync(older.Id, 99, "No action required", TestContext.Current.CancellationToken);

        var (openItems, openTotal) = await repo.ListAsync(
            ReportTicketStatus.Open,
            ct: TestContext.Current.CancellationToken
        );
        Assert.Equal(1, openTotal);
        Assert.Equal(newer.Id, Assert.Single(openItems).Id);

        var (resolvedItems, resolvedTotal) = await repo.ListAsync(
            ReportTicketStatus.Resolved,
            ct: TestContext.Current.CancellationToken
        );
        Assert.Equal(1, resolvedTotal);
        Assert.Equal(older.Id, Assert.Single(resolvedItems).Id);
    }

    [Fact]
    public async Task ResolveAsync_SetsResolvedFields()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        await using var db = new MainContext(options);
        var repo = new ReportTicketRepository(db);
        var ticket = await repo.CreateAsync(
            CreateRequest("reason"),
            TestContext.Current.CancellationToken
        );

        var resolved = await repo.ResolveAsync(
            ticket.Id,
            42,
            "Warned the reported player",
            TestContext.Current.CancellationToken
        );
        Assert.True(resolved);

        var loaded = await repo.GetByIdAsync(ticket.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal(ReportTicketStatus.Resolved, loaded.Status);
        Assert.Equal(42, loaded.ResolvedByUserId);
        Assert.NotNull(loaded.ResolvedAt);
        Assert.Equal("Warned the reported player", loaded.ResolutionAction);

        var again = await repo.ResolveAsync(
            ticket.Id,
            42,
            "Duplicate resolve",
            TestContext.Current.CancellationToken
        );
        Assert.False(again);
    }

    private static ReportTicketCreateRequest CreateRequest(string reason) =>
        new(
            1,
            "reporter",
            100,
            "ReporterChar",
            reason,
            10990100,
            1,
            "Akihabara",
            [new ReportTicketPlayerSnapshot(1, "reporter", 100, "ReporterChar")],
            []
        );
}
