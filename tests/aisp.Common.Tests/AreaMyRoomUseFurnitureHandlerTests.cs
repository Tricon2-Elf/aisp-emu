using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public class AreaMyRoomUseFurnitureHandlerTests
{
    [Fact]
    public async Task CatalogFurnitureUse_ValidatesAndBroadcastsToRoomPeers()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 42, TestContext.Current.CancellationToken);
            await using (var db = new MainContext(options))
            {
                db.Items.Add(
                    new Item
                    {
                        Id = 11_001_170,
                        Name = "Tree",
                        Socket = 0,
                    }
                );
                db.Furniture.Add(
                    new Furniture
                    {
                        ItemId = 11_001_170,
                        Type = 0,
                        PlacementFlags = FurniturePlacementFlags.Floor,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
                var room = await new MyRoomRepository(db).GetOrCreateDefaultRoomAsync(
                    42,
                    TestContext.Current.CancellationToken
                );
                Assert.NotNull(room);
                db.MyRoomFurniture.Add(
                    new MyRoomFurniture
                    {
                        RoomId = room.Id,
                        FurnitureId = 77,
                        ItemId = 11_001_170,
                        PositionX = 0,
                        PositionY = 0,
                        PositionZ = 0,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var session = new CapturingPlayerSession
            {
                MapId = MyRoomInfo.BaseMapId,
                MyRoomId = 42,
                CharacterId = 42,
                ChannelId = 1,
            };
            var peer = new CapturingPlayerSession
            {
                MapId = MyRoomInfo.BaseMapId,
                MyRoomId = 42,
                CharacterId = 99,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, session);
            state.RegisterClient(ServerType.Area, peer);

            var handler = new AreaMyRoomUseFurnitureHandler(
                new MyRoomRepository(new MainContext(options)),
                state,
                NullLogger<AreaMyRoomUseFurnitureHandler>.Instance
            );

            await handler.HandleAsync(
                BuildRequest(42, 77, 1),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(PacketType.MyRoomUseFurnitureResponse, session.Sent[0].Type);
            Assert.Equal(0u, new PacketReader(session.Sent[0].Payload).ReadUInt());
            Assert.Contains(
                session.Sent,
                packet => packet.Type == PacketType.NotifyMyRoomUseFurniture
            );
            var peerNotify = Assert.Single(
                peer.Sent,
                packet => packet.Type == PacketType.NotifyMyRoomUseFurniture
            );
            var reader = new PacketReader(peerNotify.Payload);
            Assert.Equal(42u, reader.ReadUInt());
            Assert.Equal(77u, reader.ReadUInt());
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task MissingFurniture_RejectsWithoutNotify()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 42, TestContext.Current.CancellationToken);
            var handler = new AreaMyRoomUseFurnitureHandler(
                new MyRoomRepository(new MainContext(options)),
                new SharedState(),
                NullLogger<AreaMyRoomUseFurnitureHandler>.Instance
            );
            var session = new CapturingPlayerSession
            {
                MapId = MyRoomInfo.BaseMapId,
                MyRoomId = 42,
                CharacterId = 42,
            };

            await handler.HandleAsync(
                BuildRequest(42, 77, 1),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Single(session.Sent);
            Assert.Equal(PacketType.MyRoomUseFurnitureResponse, session.Sent[0].Type);
            Assert.Equal(1u, new PacketReader(session.Sent[0].Payload).ReadUInt());
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task WrongRoom_RejectsWithoutStorage()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            var handler = new AreaMyRoomUseFurnitureHandler(
                new MyRoomRepository(new MainContext(options)),
                new SharedState(),
                NullLogger<AreaMyRoomUseFurnitureHandler>.Instance
            );
            var session = new CapturingPlayerSession
            {
                MapId = MyRoomInfo.BaseMapId,
                MyRoomId = 42,
                CharacterId = 42,
            };

            await handler.HandleAsync(
                BuildRequest(99, 77, 1),
                session,
                TestContext.Current.CancellationToken
            );

            Assert.Single(session.Sent);
            Assert.Equal(PacketType.MyRoomUseFurnitureResponse, session.Sent[0].Type);
            Assert.Equal(1u, new PacketReader(session.Sent[0].Payload).ReadUInt());
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static byte[] BuildRequest(uint roomId, uint furnId, uint reason)
    {
        var writer = new PacketWriter();
        writer.Write(roomId);
        writer.Write(furnId);
        writer.Write(reason);
        return writer.ToBytes();
    }
}
