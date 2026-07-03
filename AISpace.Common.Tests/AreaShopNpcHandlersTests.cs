using AISpace.Common.DAL;
using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISpace.Common.Tests;

public class AreaShopNpcHandlersTests
{
    private const uint StarterMapId = 10990100;
    private const uint StarterNpcObjectId = 0x50000001;
    private const uint StarterModelId = 1001021;
    private const int StarterItemId = 10100220;

    [Fact]
    public async Task NpcGetDataHandler_SendsConfiguredNpcFromDatabase()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using (var db = new MainContext(options))
            {
                await SeedShopNpcAsync(db, isNpcEnabled: true, isShopEnabled: true, isShopItemEnabled: true);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var runDb = new MainContext(options);
            var handler = new AreaNpcGetDataHandler(new NpcRepository(runDb));
            var session = new CapturingPlayerSession { MapId = StarterMapId };

            await handler.HandleAsync(ReadOnlyMemory<byte>.Empty, session, TestContext.Current.CancellationToken);

            Assert.Contains(session.Sent, p => p.Type == PacketType.NpcGetDataResponse);
            var npcPacket = Assert.Single(session.Sent, p => p.Type == PacketType.NpcNotifyData);
            var reader = new PacketReader(npcPacket.Payload);
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(StarterNpcObjectId, reader.ReadUInt());
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task EventAccessNpcHandler_UsesDatabaseShopBannerAndItems()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            const uint bannerVisualId = 10110;

            await using (var db = new MainContext(options))
            {
                await SeedShopNpcAsync(db, isNpcEnabled: true, isShopEnabled: true, isShopItemEnabled: true, bannerVisualId: bannerVisualId);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var runDb = new MainContext(options);
            var handler = new AreaEventAccessNpcHandler(
                new NpcRepository(runDb),
                new ShopRepository(runDb),
                NullLogger<AreaEventAccessNpcHandler>.Instance
            );
            var session = new CapturingPlayerSession
            {
                MapId = StarterMapId,
                CharacterId = 9001,
            };

            await handler.HandleAsync(BuildEventAccessNpcPayload(StarterNpcObjectId), session, TestContext.Current.CancellationToken);

            Assert.Contains(session.Sent, p => p.Type == PacketType.EventAccessNpcResponse);
            Assert.Contains(session.Sent, p => p.Type == PacketType.NotifySupplyNpcExec);
            var shopStarted = Assert.Single(session.Sent, p => p.Type == PacketType.ShopStartedNotify);
            var startedReader = new PacketReader(shopStarted.Payload);
            Assert.Equal(StarterNpcObjectId, startedReader.ReadUInt());
            Assert.Equal("Starter Shop", startedReader.ReadString("ASCII"));
            Assert.Equal(bannerVisualId, startedReader.ReadUInt());
            Assert.Equal(0u, startedReader.ReadUInt());

            var shopItems = Assert.Single(session.Sent, p => p.Type == PacketType.ShopItemNotify);
            var itemReader = new PacketReader(shopItems.Payload);
            Assert.Equal(1u, itemReader.ReadUInt());
            Assert.Equal(50UL, itemReader.ReadULong());
            Assert.Equal(50UL, itemReader.ReadULong());
            Assert.Equal((uint)StarterItemId, itemReader.ReadUInt());
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task EventAccessNpcHandler_RejectsDisabledNpc()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using (var db = new MainContext(options))
            {
                await SeedShopNpcAsync(db, isNpcEnabled: false, isShopEnabled: true, isShopItemEnabled: true);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var runDb = new MainContext(options);
            var handler = new AreaEventAccessNpcHandler(
                new NpcRepository(runDb),
                new ShopRepository(runDb),
                NullLogger<AreaEventAccessNpcHandler>.Instance
            );
            var session = new CapturingPlayerSession
            {
                MapId = StarterMapId,
                CharacterId = 9001,
            };

            await handler.HandleAsync(BuildEventAccessNpcPayload(StarterNpcObjectId), session, TestContext.Current.CancellationToken);

            var response = Assert.Single(session.Sent, p => p.Type == PacketType.EventAccessNpcResponse);
            var reader = new PacketReader(response.Payload);
            Assert.Equal(1u, reader.ReadUInt());
            Assert.DoesNotContain(session.Sent, p => p.Type == PacketType.ShopStartedNotify);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task NpcHandlers_RespectChannelRestrictions()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await using (var db = new MainContext(options))
            {
                await SeedShopNpcAsync(db, isNpcEnabled: true, isShopEnabled: true, isShopItemEnabled: true, channelId: 1);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var runDb = new MainContext(options);
            var npcHandler = new AreaNpcGetDataHandler(new NpcRepository(runDb));
            var accessHandler = new AreaEventAccessNpcHandler(
                new NpcRepository(runDb),
                new ShopRepository(runDb),
                NullLogger<AreaEventAccessNpcHandler>.Instance
            );

            var offChannelSession = new CapturingPlayerSession
            {
                MapId = StarterMapId,
                ChannelId = 2,
                CharacterId = 9001,
            };

            await npcHandler.HandleAsync(ReadOnlyMemory<byte>.Empty, offChannelSession, TestContext.Current.CancellationToken);
            Assert.DoesNotContain(offChannelSession.Sent, p => p.Type == PacketType.NpcNotifyData);

            await accessHandler.HandleAsync(BuildEventAccessNpcPayload(StarterNpcObjectId), offChannelSession, TestContext.Current.CancellationToken);
            var response = Assert.Single(offChannelSession.Sent, p => p.Type == PacketType.EventAccessNpcResponse);
            var reader = new PacketReader(response.Payload);
            Assert.Equal(1u, reader.ReadUInt());
            Assert.DoesNotContain(offChannelSession.Sent, p => p.Type == PacketType.ShopStartedNotify);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static async Task SeedShopNpcAsync(
        MainContext db,
        bool isNpcEnabled,
        bool isShopEnabled,
        bool isShopItemEnabled,
        uint bannerVisualId = 10110,
        int channelId = -1
    )
    {
        db.Items.Add(new Item { Id = StarterItemId, Name = "Starter Item", Socket = 8 });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var shop = new Shop
        {
            Code = "starter_clothing",
            DisplayName = "Starter Shop",
            BannerVisualId = bannerVisualId,
            IsEnabled = isShopEnabled,
        };
        db.Shops.Add(shop);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.ShopItems.Add(
            new ShopItem
            {
                ShopId = shop.Id,
                ItemId = StarterItemId,
                AiPrice = 50,
                NicoPrice = 50,
                SortOrder = 0,
                IsEnabled = isShopItemEnabled,
            }
        );

        var npc = new Npc
        {
            MapId = StarterMapId,
            ChannelId = channelId,
            DayPhase = -1,
            DateStartUtc = DateTime.UnixEpoch,
            DateEndUtc = DateTime.MaxValue,
            NpcObjectId = StarterNpcObjectId,
            ModelId = StarterModelId,
            Name = "Starter Shop NPC",
            X = -9000f,
            Y = 2f,
            Z = -17900f,
            Rotation = 90,
            ShopId = shop.Id,
            InteractionType = NpcInteractionType.Shop,
            IsEnabled = isNpcEnabled,
            SortOrder = 0,
        };
        db.Npcs.Add(npc);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.NpcEquipments.Add(
            new NpcEquipment
            {
                NpcId = npc.Id,
                SlotIndex = 0,
                ItemId = StarterItemId,
                SortOrder = 0,
            }
        );
    }

    private static byte[] BuildEventAccessNpcPayload(uint npcObjectId)
    {
        var writer = new PacketWriter();
        writer.Write(npcObjectId);
        writer.Write(-9000f);
        writer.Write(2f);
        writer.Write(-17900f);
        return writer.ToBytes();
    }
}
