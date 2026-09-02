using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public class AdventureWorkHandlersTests
{
    private static async Task<User> SeedUserAsync(MainContext db)
    {
        var user = new User { Username = "drama", AdventureSheetStock = 20 };
        user.SetPassword("secret");
        db.Users.Add(user);
        await db.SaveChangesAsync();
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
            await db.SaveChangesAsync();
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

    [Fact]
    public async Task ShopAndUploadEndHandlers_AcknowledgeWithFourByteResult()
    {
        var session = new CapturingPlayerSession();

        await new AreaAdventureUploadEndHandler().HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );
        await new AreaAdventureShopEndHandler().HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        // Both windows stay open until the client reads a fixed 4-byte result.
        var upload = Assert.Single(
            session.Sent,
            p => p.Type == PacketType.AdventureUploadEndResponse
        );
        Assert.Equal(4, upload.Payload.Length);
        Assert.Equal(0u, new PacketReader(upload.Payload).ReadUInt());

        var shop = Assert.Single(session.Sent, p => p.Type == PacketType.AdventureShopEndResponse);
        Assert.Equal(4, shop.Payload.Length);
        Assert.Equal(0u, new PacketReader(shop.Payload).ReadUInt());
        // The shop window only closes on the empty "ended" notify that follows the reply.
        var ended = Assert.Single(session.Sent, p => p.Type == PacketType.AdventureShopEndedNotify);
        Assert.Empty(ended.Payload);
        Assert.True(session.Sent.IndexOf(shop) < session.Sent.IndexOf(ended));
    }

    private static byte[] UploadRequestBytes(ushort workId, long contentSize = 20348)
    {
        var writer = new PacketWriter();
        writer.Write(workId);
        writer.Write("Thimbleglow");
        writer.Write(2u);
        writer.Write("");
        writer.Write("Yomogi");
        writer.Write(1000ul);
        writer.Write((byte)1);
        writer.Write((ulong)contentSize);
        return writer.ToBytes();
    }

    [Fact]
    public async Task UploadRequestHandler_RefusesUnknownWorkWithFixedSizeReply()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var user = await SeedUserAsync(db);
            var session = new CapturingPlayerSession { UserId = user.Id, CharacterId = 1 };

            await new AreaAdventureUploadRequestHandler(
                new AdventureShopRepository(db),
                NullLogger<AreaAdventureUploadRequestHandler>.Instance
            ).HandleAsync(UploadRequestBytes(7), session, TestContext.Current.CancellationToken);

            // The client reads a fixed 55-byte body: result, workId, scriptId, 41-byte ticket.
            var reply = Assert.Single(
                session.Sent,
                p => p.Type == PacketType.AdventureUploadRequestResponse
            );
            Assert.Equal(55, reply.Payload.Length);
            var reader = new PacketReader(reply.Payload);
            Assert.Equal(AreaAdventureUploadRequestHandler.RefusedResult, reader.ReadUInt());
            Assert.Equal(7, reader.ReadUShort());
            Assert.Equal(0ul, reader.ReadULong());
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task UploadRequestThenReport_ListsTheWorkOnceTheManuscriptIsStored()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var user = await SeedUserAsync(db);
            var session = new CapturingPlayerSession { UserId = user.Id, CharacterId = 1 };
            var works = new AdventureWorkRepository(db);
            var shop = new AdventureShopRepository(db);
            await new AreaAdventureWorkCreateHandler(works).HandleAsync(
                new byte[] { 1, 0, 0, 0 },
                session,
                TestContext.Current.CancellationToken
            );
            session.Sent.Clear();

            await new AreaAdventureUploadRequestHandler(
                shop,
                NullLogger<AreaAdventureUploadRequestHandler>.Instance
            ).HandleAsync(UploadRequestBytes(1), session, TestContext.Current.CancellationToken);

            var reply = Assert.Single(session.Sent);
            Assert.Equal(55, reply.Payload.Length);
            var reader = new PacketReader(reply.Payload);
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(1, reader.ReadUShort());
            var scriptId = (long)reader.ReadULong();
            Assert.Equal(AdventureListing.FirstScriptId, scriptId);
            var ticket = reader.ReadFixedString(AdventureUploadRequestResponse.TicketLength);
            Assert.Equal(AdventureShopRepository.TicketLength, ticket.Length);

            // Reporting success before upload.php stored anything must not list the work.
            var report = new AreaAdventureUploadRequestReportHandler(
                shop,
                NullLogger<AreaAdventureUploadRequestReportHandler>.Instance
            );
            session.Sent.Clear();
            await report.HandleAsync(
                ReportBytes(1, 1, scriptId),
                session,
                TestContext.Current.CancellationToken
            );
            var early = new PacketReader(Assert.Single(session.Sent).Payload);
            Assert.Equal(AreaAdventureUploadRequestReportHandler.NotListedResult, early.ReadUInt());
            Assert.Equal((ulong)scriptId, early.ReadULong());

            // upload.php: the ticket is single-use and yields the pending listing.
            var pending = await shop.RedeemUploadTicketAsync(
                ticket,
                TestContext.Current.CancellationToken
            );
            Assert.NotNull(pending);
            Assert.Equal(scriptId, pending.ScriptId);
            Assert.Null(
                await shop.RedeemUploadTicketAsync(ticket, TestContext.Current.CancellationToken)
            );
            Assert.True(
                await shop.StoreContentAsync(
                    scriptId,
                    "ADV0..."u8.ToArray(),
                    "list"u8.ToArray(),
                    ct: TestContext.Current.CancellationToken
                )
            );

            session.Sent.Clear();
            await report.HandleAsync(
                ReportBytes(1, 1, scriptId),
                session,
                TestContext.Current.CancellationToken
            );
            var ok = new PacketReader(Assert.Single(session.Sent).Payload);
            Assert.Equal(0u, ok.ReadUInt());
            Assert.Equal((ulong)scriptId, ok.ReadULong());

            var (_, listedWorks) = await works.GetWorksAsync(
                user.Id,
                TestContext.Current.CancellationToken
            );
            Assert.True(Assert.Single(listedWorks).Uploaded);
            var uploads = await shop.GetUploadListAsync(
                user.Id,
                TestContext.Current.CancellationToken
            );
            var listing = Assert.Single(uploads);
            Assert.Equal(AdventureListingState.Listed, listing.State);
            Assert.Equal("Thimbleglow", listing.Title);
            Assert.Equal(1000, listing.Price);

            // Taking it down frees the work for another upload, which gets a fresh id.
            Assert.True(
                await shop.DelistAsync(user.Id, scriptId, TestContext.Current.CancellationToken)
            );
            (_, listedWorks) = await works.GetWorksAsync(
                user.Id,
                TestContext.Current.CancellationToken
            );
            Assert.False(Assert.Single(listedWorks).Uploaded);
            var again = await shop.BeginUploadAsync(
                user.Id,
                1,
                1,
                new AdventureListingDraft("Again", "Yomogi", 2, "", 500, true, 100),
                TestContext.Current.CancellationToken
            );
            Assert.NotNull(again);
            Assert.Equal(scriptId + 1, again.Value.Listing.ScriptId);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static byte[] ReportBytes(uint report, ushort workId, long scriptId)
    {
        var writer = new PacketWriter();
        writer.Write(report);
        writer.Write(workId);
        writer.Write((ulong)scriptId);
        return writer.ToBytes();
    }

    [Fact]
    public async Task UploadRequestReportHandler_FailureDropsThePendingListing()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using var db = new MainContext(options);
            var user = await SeedUserAsync(db);
            var session = new CapturingPlayerSession { UserId = user.Id, CharacterId = 1 };
            var shop = new AdventureShopRepository(db);
            await new AreaAdventureWorkCreateHandler(new AdventureWorkRepository(db)).HandleAsync(
                new byte[] { 1, 0, 0, 0 },
                session,
                TestContext.Current.CancellationToken
            );
            var started = await shop.BeginUploadAsync(
                user.Id,
                1,
                1,
                new AdventureListingDraft("T", "A", 0, "", 10, true, 1),
                TestContext.Current.CancellationToken
            );
            Assert.NotNull(started);
            session.Sent.Clear();

            await new AreaAdventureUploadRequestReportHandler(
                shop,
                NullLogger<AreaAdventureUploadRequestReportHandler>.Instance
            ).HandleAsync(
                ReportBytes(0, 1, started.Value.Listing.ScriptId),
                session,
                TestContext.Current.CancellationToken
            );

            var reply = Assert.Single(
                session.Sent,
                p => p.Type == PacketType.AdventureUploadRequestReportResponse
            );
            Assert.Equal(12, reply.Payload.Length);
            var reader = new PacketReader(reply.Payload);
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal((ulong)started.Value.Listing.ScriptId, reader.ReadULong());
            Assert.Empty(db.AdventureListings);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
