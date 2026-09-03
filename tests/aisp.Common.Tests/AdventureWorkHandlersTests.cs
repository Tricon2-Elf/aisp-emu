using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;

namespace aisp.Common.Tests;

public class AdventureWorkHandlersTests
{
    private static async Task<User> SeedUserAsync(MainContext db)
    {
        var user = new User { Username = "drama", AdventureSheetStock = 20 };
        user.SetPassword("secret");
        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user;
    }

    [Fact]
    public async Task WorkList_EmptyAccount_SendsStockAndNoRecords()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var user = await SeedUserAsync(db);
            var session = new CapturingPlayerSession { UserId = user.Id, CharacterId = 1 };
            var handler = new AreaGetAdventureWorkListHandler(new AdventureWorkRepository(db));

            await handler.HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                session,
                TestContext.Current.CancellationToken
            );

            var sent = Assert.Single(session.Sent);
            Assert.Equal(PacketType.GetAdventureWorkListResponse, sent.Type);
            var reader = new PacketReader(sent.Payload);
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(20u, reader.ReadUInt());
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(12, sent.Payload.Length);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task WorkCreate_AllocatesMonotonicIds_ConsumesSheets_AndListsPackedRecords()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var user = await SeedUserAsync(db);
            var repo = new AdventureWorkRepository(db);
            var session = new CapturingPlayerSession { UserId = user.Id, CharacterId = 1 };
            var create = new AreaAdventureWorkCreateHandler(repo);

            await create.HandleAsync(
                new byte[] { 1, 0, 0, 0 },
                session,
                TestContext.Current.CancellationToken
            );
            await create.HandleAsync(
                new byte[] { 2, 0, 0, 0 },
                session,
                TestContext.Current.CancellationToken
            );

            // The stock push first (the client refreshes its display when the reply lands), then
            // recv_adventure_work_create_r: u32 result, u32 sheets, u16 work id.
            Assert.Equal(PacketType.AdventureUpdatedSheetStackNotify, session.Sent[0].Type);
            Assert.Equal(19u, new PacketReader(session.Sent[0].Payload).ReadUInt());
            var first = session.Sent[1];
            Assert.Equal(PacketType.AdventureWorkCreateResponse, first.Type);
            Assert.Equal(10, first.Payload.Length);
            var r = new PacketReader(first.Payload);
            Assert.Equal(0u, r.ReadUInt());
            Assert.Equal(1u, r.ReadUInt());
            Assert.Equal((ushort)1, r.ReadUShort());

            var second = new PacketReader(session.Sent[3].Payload);
            second.ReadUInt();
            Assert.Equal(2u, second.ReadUInt());
            Assert.Equal((ushort)2, second.ReadUShort());

            // Deleting work 1 must not let id 1 be handed out again.
            await new AreaAdventureWorkDeleteHandler(repo).HandleAsync(
                new byte[] { 1, 0 },
                session,
                TestContext.Current.CancellationToken
            );
            await create.HandleAsync(
                new byte[] { 1, 0, 0, 0 },
                session,
                TestContext.Current.CancellationToken
            );
            var third = new PacketReader(session.Sent[^1].Payload);
            third.ReadUInt();
            third.ReadUInt();
            Assert.Equal((ushort)3, third.ReadUShort());

            session.Sent.Clear();
            await new AreaGetAdventureWorkListHandler(repo).HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                session,
                TestContext.Current.CancellationToken
            );
            var list = Assert.Single(session.Sent);
            Assert.Equal(12 + 2 * 13, list.Payload.Length);
            var lr = new PacketReader(list.Payload);
            Assert.Equal(0u, lr.ReadUInt());
            Assert.Equal(17u, lr.ReadUInt());
            Assert.Equal(2u, lr.ReadUInt());
            Assert.Equal(2u, lr.ReadUInt());
            Assert.Equal(2u, lr.ReadUInt());
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task AddSheet_RepliesWithDelta_AndRefusesBeyondStock()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var user = await SeedUserAsync(db);
            var repo = new AdventureWorkRepository(db);
            var session = new CapturingPlayerSession { UserId = user.Id, CharacterId = 1 };
            await new AreaAdventureWorkCreateHandler(repo).HandleAsync(
                new byte[] { 1, 0, 0, 0 },
                session,
                TestContext.Current.CancellationToken
            );
            session.Sent.Clear();

            var add = new AreaAdventureWorkAddSheetHandler(repo);
            await add.HandleAsync(
                new byte[] { 1, 0, 10, 0, 0, 0 },
                session,
                TestContext.Current.CancellationToken
            );

            // Stock push first, then recv_adventure_work_add_sheet_r: u32 result, u16 work id, u32 delta
            // (the client adds it to its local count, so not the total).
            Assert.Equal(PacketType.AdventureUpdatedSheetStackNotify, session.Sent[0].Type);
            Assert.Equal(9u, new PacketReader(session.Sent[0].Payload).ReadUInt());
            var reply = new PacketReader(session.Sent[1].Payload);
            Assert.Equal(0u, reply.ReadUInt());
            Assert.Equal((ushort)1, reply.ReadUShort());
            Assert.Equal(10u, reply.ReadUInt());

            session.Sent.Clear();
            await add.HandleAsync(
                new byte[] { 1, 0, 50, 0, 0, 0 },
                session,
                TestContext.Current.CancellationToken
            );
            var refused = Assert.Single(session.Sent);
            Assert.Equal(1u, new PacketReader(refused.Payload).ReadUInt());
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Register_KeepsRestoredIdsAndMovesCounterPastThem()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var user = await SeedUserAsync(db);
            var repo = new AdventureWorkRepository(db);

            var restored = await repo.RegisterAsync(
                user.Id,
                1,
                7,
                11,
                TestContext.Current.CancellationToken
            );
            Assert.NotNull(restored);
            Assert.Equal(7, restored.WorkId);
            Assert.Equal(11, restored.Sheets);

            var (created, stock) = await repo.CreateAsync(
                user.Id,
                1,
                1,
                TestContext.Current.CancellationToken
            );
            Assert.NotNull(created);
            Assert.Equal(8, created.WorkId);
            Assert.Equal(19, stock);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Stock_StartsAtZero_AndBuyingSheetsRaisesIt()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var user = new User { Username = "fresh", AiPoints = 50 };
            user.SetPassword("secret");
            db.Users.Add(user);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            var repo = new AdventureWorkRepository(db);

            var (none, stock) = await repo.CreateAsync(
                user.Id,
                1,
                1,
                TestContext.Current.CancellationToken
            );
            Assert.Null(none);
            Assert.Equal(0, stock);

            Assert.Equal(
                (5, 0L),
                await repo.BuySheetsAsync(user.Id, 5, 10, TestContext.Current.CancellationToken)
            );
            Assert.Null(
                await repo.BuySheetsAsync(user.Id, 1, 10, TestContext.Current.CancellationToken)
            );
            var (created, after) = await repo.CreateAsync(
                user.Id,
                1,
                1,
                TestContext.Current.CancellationToken
            );
            Assert.NotNull(created);
            Assert.Equal(4, after);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
