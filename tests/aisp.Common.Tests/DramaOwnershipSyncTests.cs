using System.Buffers.Binary;
using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public sealed class DramaOwnershipSyncTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(14100000, 257, 0, false)]
    [InlineData(172001001, 32001001, 2, false)]
    [InlineData(182001001, 42001001, 3, false)]
    [InlineData(143010001, 10001, 1, false)]
    [InlineData(14100000, 257, 0, true)]
    [InlineData(172001001, 32001001, 2, true)]
    [InlineData(182001001, 42001001, 3, true)]
    [InlineData(143010001, 10001, 1, true)]
    public async Task Discard_refreshes_affected_registries_only_when_last_copy_is_removed(
        int itemId,
        uint registryId,
        int kind,
        bool trashbox
    )
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var lifetime = connection;
        await TestDb.SeedCharacterAsync(options, 9001, Ct);
        await using var db = new MainContext(options);
        var session = await SeedAsync(db, itemId);
        var catalog = new DramaCatalog(db, TestTextLocaliser.English);
        var responseType = trashbox
            ? PacketType.TrashboxDiscardItemResponse
            : PacketType.ItemDiscardResponse;

        foreach (var quantity in new ushort[] { 3, 1, 1 })
        {
            session.Sent.Clear();
            // A separate scope changes the DB while the catalog scope and session retain old inventory objects.
            await using var mutationDb = new MainContext(options);
            var characters = new CharacterRepository(
                mutationDb,
                NullLogger<CharacterRepository>.Instance
            );
            IPacketHandler handler = trashbox
                ? new AreaTrashboxDiscardItemHandler(
                    characters,
                    NullLogger<AreaTrashboxDiscardItemHandler>.Instance,
                    catalog
                )
                : new ItemDiscardHandler(
                    characters,
                    NullLogger<ItemDiscardHandler>.Instance,
                    catalog
                );
            var packet = new PacketWriter();
            if (trashbox)
                packet.Write(1u);
            packet.Write((uint)itemId);
            if (trashbox)
                packet.Write(1u);
            packet.Write(quantity);
            await handler.HandleAsync(packet.ToBytes(), session, Ct);
            Assert.Equal(responseType, session.Sent[^1].Type);
            Assert.Equal(
                quantity == 3 ? 1u : 0u,
                new PacketReader(session.Sent[^1].Payload).ReadUInt()
            );
            var remaining = await mutationDb
                .CharacterInventories.Where(x => x.CharacterId == 9001 && x.ItemId == itemId)
                .Select(x => x.Quantity)
                .SingleOrDefaultAsync(Ct);
            AssertRefresh(session, kind, registryId, changed: remaining == 0, owned: false);
        }
    }

    [Theory]
    [InlineData(14100000, 257, 0)]
    [InlineData(172001001, 32001001, 2)]
    [InlineData(182001001, 42001001, 3)]
    [InlineData(143010001, 10001, 1)]
    public async Task Warehouse_transfers_refresh_on_last_copy_out_and_first_copy_back(
        int itemId,
        uint registryId,
        int kind
    )
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var lifetime = connection;
        await TestDb.SeedCharacterAsync(options, 9001, Ct);
        await using var db = new MainContext(options);
        var session = await SeedAsync(db, itemId);
        var catalog = new DramaCatalog(db, TestTextLocaliser.English);
        foreach (
            var (toStorage, changed) in new[]
            {
                (true, false),
                (true, true),
                (false, true),
                (false, false),
            }
        )
        {
            session.Sent.Clear();
            await using var mutationDb = new MainContext(options);
            var handler = new AreaItemMoveHandler(
                new UserRepository(mutationDb),
                NullLogger<AreaItemMoveHandler>.Instance,
                catalog
            );
            var packet = new PacketWriter();
            packet.Write(toStorage ? 0u : 1u);
            packet.Write((uint)itemId);
            packet.Write((ushort)1);
            packet.Write(toStorage ? 1u : 0u);
            packet.Write(0u);
            await handler.HandleAsync(packet.ToBytes(), session, Ct);
            Assert.Equal(PacketType.ItemMoveResponse, session.Sent[^1].Type);
            Assert.Equal(0u, new PacketReader(session.Sent[^1].Payload).ReadUInt());
            AssertRefresh(session, kind, registryId, changed, owned: !toStorage);
        }
    }

    private static async Task<CapturingPlayerSession> SeedAsync(MainContext db, int itemId)
    {
        await DramaTestCatalog.SeedAsync(db);
        db.CharacterInventories.Add(
            new CharacterInventory
            {
                CharacterId = 9001,
                ItemId = itemId,
                Quantity = 2,
            }
        );
        await db.SaveChangesAsync(Ct);
        return new CapturingPlayerSession
        {
            CharacterId = 9001,
            User = await db.Users.SingleAsync(Ct),
            Character = await db
                .Characters.AsNoTracking()
                .Include(x => x.Inventory)
                .SingleAsync(Ct),
        };
    }

    private static void AssertRefresh(
        CapturingPlayerSession session,
        int kind,
        uint registryId,
        bool changed,
        bool owned
    )
    {
        var refreshes = session
            .Sent.Where(x =>
                x.Type
                    is PacketType.UccAdvFigureBaseListResponse
                        or PacketType.NiconiCommonsBaseListResponse
                        or PacketType.UccVoiceBaseListResponse
            )
            .ToArray();
        if (!changed)
        {
            Assert.Empty(refreshes);
            return;
        }
        PacketType[] expected = kind switch
        {
            0 =>
            [
                PacketType.UccAdvFigureBaseListResponse,
                PacketType.NiconiCommonsBaseListResponse,
            ],
            1 => [PacketType.UccVoiceBaseListResponse],
            _ => [PacketType.NiconiCommonsBaseListResponse],
        };
        Assert.Equal(expected, refreshes.Select(x => x.Type).ToArray());
        var bytes = refreshes[0].Payload;
        var (rowSize, ownedOffset) = kind switch
        {
            0 => (377, 104),
            1 => (874, 104),
            _ => (113, 108),
        };
        var count = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4));
        var flags = Enumerable
            .Range(0, (int)count)
            .ToDictionary(
                i => BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(8 + i * rowSize)),
                i => bytes[8 + i * rowSize + ownedOffset] != 0
            );
        Assert.Equal(owned, flags[registryId]);
        if (kind == 0)
            Assert.True(flags[1000]); // Licensed figures remain available without an inventory item.
        Assert.Null(session.ActiveShopId); // Neither the shop nor the editor had to request a refresh.
    }
}
