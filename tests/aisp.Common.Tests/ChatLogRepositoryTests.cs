using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Tests.Support;

namespace aisp.Common.Tests;

public sealed class ChatLogRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsMessageForLaterListing()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;

        await using (var db = new MainContext(options))
        {
            var repo = new ChatLogRepository(db);
            await repo.AddAsync(
                new ChatMessage
                {
                    Kind = ChatLogKind.Public,
                    UserId = 2,
                    CharacterId = 9001,
                    CharacterName = "char9001",
                    Message = "hello later",
                    DistId = 0,
                    MapId = 10990100,
                    ChannelId = 1,
                },
                TestContext.Current.CancellationToken
            );
        }

        await using (var db = new MainContext(options))
        {
            var repo = new ChatLogRepository(db);
            var (items, total) = await repo.ListAsync(ct: TestContext.Current.CancellationToken);
            Assert.Equal(1, total);
            var row = Assert.Single(items);
            Assert.Equal(ChatLogKind.Public, row.Kind);
            Assert.Equal(2, row.UserId);
            Assert.Equal(9001, row.CharacterId);
            Assert.Equal("char9001", row.CharacterName);
            Assert.Equal("hello later", row.Message);
            Assert.Equal(10990100u, row.MapId);
            Assert.Equal(1, row.ChannelId);
            Assert.False(row.Rejected);
        }
    }

    [Fact]
    public async Task ListAsync_FiltersAndReturnsNewestFirst()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        await using var db = new MainContext(options);
        var repo = new ChatLogRepository(db);

        await repo.AddAsync(
            new ChatMessage
            {
                Kind = ChatLogKind.Public,
                UserId = 1,
                CharacterId = 1,
                CharacterName = "a",
                Message = "first",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            },
            TestContext.Current.CancellationToken
        );
        await repo.AddAsync(
            new ChatMessage
            {
                Kind = ChatLogKind.Circle,
                UserId = 1,
                CharacterId = 1,
                CharacterName = "a",
                Message = "circle",
                CircleId = 7,
                CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            },
            TestContext.Current.CancellationToken
        );
        await repo.AddAsync(
            new ChatMessage
            {
                Kind = ChatLogKind.Public,
                UserId = 2,
                CharacterId = 2,
                CharacterName = "b",
                Message = "slur",
                Rejected = true,
                CreatedAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc),
            },
            TestContext.Current.CancellationToken
        );

        var (circle, circleTotal) = await repo.ListAsync(
            kind: ChatLogKind.Circle,
            ct: TestContext.Current.CancellationToken
        );
        Assert.Equal(1, circleTotal);
        Assert.Equal("circle", Assert.Single(circle).Message);

        var (rejected, rejectedTotal) = await repo.ListAsync(
            rejected: true,
            ct: TestContext.Current.CancellationToken
        );
        Assert.Equal(1, rejectedTotal);
        Assert.Equal("slur", Assert.Single(rejected).Message);

        var (page, total) = await repo.ListAsync(
            skip: 0,
            take: 2,
            ct: TestContext.Current.CancellationToken
        );
        Assert.Equal(3, total);
        Assert.Equal(["slur", "circle"], page.Select(x => x.Message).ToArray());
    }

    [Fact]
    public async Task PruneOlderThanAsync_DeletesOnlyRowsBeforeCutoff()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        await using var db = new MainContext(options);
        var repo = new ChatLogRepository(db);
        var now = DateTime.UtcNow;

        await repo.AddAsync(
            new ChatMessage
            {
                Kind = ChatLogKind.Public,
                UserId = 1,
                CharacterId = 1,
                CharacterName = "a",
                Message = "old",
                CreatedAt = now.AddDays(-61),
            },
            TestContext.Current.CancellationToken
        );
        await repo.AddAsync(
            new ChatMessage
            {
                Kind = ChatLogKind.Public,
                UserId = 1,
                CharacterId = 1,
                CharacterName = "a",
                Message = "kept",
                CreatedAt = now.AddDays(-10),
            },
            TestContext.Current.CancellationToken
        );

        var removed = await repo.PruneOlderThanAsync(
            now.AddDays(-60),
            TestContext.Current.CancellationToken
        );
        Assert.Equal(1, removed);

        var (items, total) = await repo.ListAsync(ct: TestContext.Current.CancellationToken);
        Assert.Equal(1, total);
        Assert.Equal("kept", Assert.Single(items).Message);
    }

    [Fact]
    public async Task ListRecentOnMapAsync_ReturnsOnlyPublicChatOnMapSinceCutoff()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        await using var db = new MainContext(options);
        var repo = new ChatLogRepository(db);
        var now = DateTime.UtcNow;

        await repo.AddAsync(
            new ChatMessage
            {
                Kind = ChatLogKind.Public,
                UserId = 1,
                CharacterId = 1,
                CharacterName = "a",
                Message = "recent",
                MapId = 10990100,
                ChannelId = 1,
                CreatedAt = now.AddMinutes(-2),
            },
            TestContext.Current.CancellationToken
        );
        await repo.AddAsync(
            new ChatMessage
            {
                Kind = ChatLogKind.Public,
                UserId = 1,
                CharacterId = 1,
                CharacterName = "a",
                Message = "old",
                MapId = 10990100,
                ChannelId = 1,
                CreatedAt = now.AddMinutes(-10),
            },
            TestContext.Current.CancellationToken
        );
        await repo.AddAsync(
            new ChatMessage
            {
                Kind = ChatLogKind.Circle,
                UserId = 1,
                CharacterId = 1,
                CharacterName = "a",
                Message = "circle",
                CircleId = 3,
                MapId = 10990100,
                ChannelId = 1,
                CreatedAt = now.AddMinutes(-1),
            },
            TestContext.Current.CancellationToken
        );
        await repo.AddAsync(
            new ChatMessage
            {
                Kind = ChatLogKind.Public,
                UserId = 2,
                CharacterId = 2,
                CharacterName = "b",
                Message = "other channel",
                MapId = 10990100,
                ChannelId = 2,
                CreatedAt = now.AddMinutes(-1),
            },
            TestContext.Current.CancellationToken
        );

        var items = await repo.ListRecentOnMapAsync(
            10990100,
            1,
            now.AddMinutes(-5),
            TestContext.Current.CancellationToken
        );
        Assert.Single(items);
        Assert.Equal("recent", Assert.Single(items).Message);
    }
}
