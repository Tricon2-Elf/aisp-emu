using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Data;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public class AreaRoboFurnactHandlerTests
{
    [Fact]
    public async Task FurnactStart_BroadcastsNotifyToRoomPeers()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            const int characterId = 42;
            const uint furnitureId = 7;
            await TestDb.SeedCharacterAsync(
                options,
                characterId,
                TestContext.Current.CancellationToken
            );

            await using (var db = new MainContext(options))
            {
                db.Items.Add(
                    new Item
                    {
                        Id = 11_000_000,
                        Name = "Bed",
                        Socket = 0,
                    }
                );
                db.Furniture.Add(
                    new Furniture
                    {
                        ItemId = 11_000_000,
                        Type = 0,
                        PlacementFlags = FurniturePlacementFlags.Floor,
                    }
                );
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);

                var room = await new MyRoomRepository(db).GetOrCreateDefaultRoomAsync(
                    characterId,
                    TestContext.Current.CancellationToken
                );
                Assert.NotNull(room);
                db.MyRoomFurniture.Add(
                    new MyRoomFurniture
                    {
                        RoomId = room.Id,
                        FurnitureId = furnitureId,
                        ItemId = 11_000_000,
                        PositionX = 1,
                        PositionY = 0,
                        PositionZ = 2,
                        DirectionX = 0,
                        DirectionY = 0,
                    }
                );
                var roboCharacter = new CharaData(
                    RoboRepository.GetObjectId((uint)characterId, 1),
                    1002011,
                    "Furnact Robo"
                );
                await new RoboRepository(db).UpsertAsync(
                    characterId,
                    new RoboData(1, roboCharacter) { OwnerAvatarId = (uint)characterId },
                    TestContext.Current.CancellationToken
                );
            }

            var state = new SharedState();
            var actor = new CapturingPlayerSession
            {
                CharacterId = (uint)characterId,
                MapId = MyRoomInfo.BaseMapId,
                MyRoomId = (uint)characterId,
                ChannelId = 1,
            };
            actor.AccompanyingRoboIds.Add(1);
            var peer = new CapturingPlayerSession
            {
                CharacterId = 99,
                MapId = MyRoomInfo.BaseMapId,
                MyRoomId = (uint)characterId,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, actor);
            state.RegisterClient(ServerType.Area, peer);

            var start = new MovementData(1.5f, 0f, 2.5f, 90, MovementType.Stopped);
            var handler = new AreaRoboFurnactStartHandler(
                new RoboRepository(new MainContext(options)),
                new MyRoomRepository(new MainContext(options)),
                state
            );
            await handler.HandleAsync(
                BuildFurnactStartPayload(1, furnitureId, start),
                actor,
                TestContext.Current.CancellationToken
            );

            Assert.Contains(actor.Sent, packet => packet.Type == PacketType.NotifyRoboFurnactStart);
            Assert.DoesNotContain(
                actor.Sent,
                packet =>
                    packet.Type
                        is PacketType.ItemTryEquipReplaceResponse
                            or PacketType.AvatarNotifyData
            );
            var notify = Assert.Single(
                peer.Sent,
                packet => packet.Type == PacketType.NotifyRoboFurnactStart
            );
            var reader = new PacketReader(notify.Payload);
            Assert.Equal(1u, reader.ReadUInt());
            Assert.Equal(furnitureId, reader.ReadUInt());
            var move = MovementData.FromBytes(reader.ReadBytes(14));
            Assert.Equal(start.X, move.X);
            Assert.Equal(start.Z, move.Z);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task FurnactEnd_BroadcastsNotifyToRoomPeers()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            const int characterId = 42;
            await TestDb.SeedCharacterAsync(
                options,
                characterId,
                TestContext.Current.CancellationToken
            );
            await using (var db = new MainContext(options))
            {
                var roboCharacter = new CharaData(
                    RoboRepository.GetObjectId((uint)characterId, 1),
                    1002011,
                    "Furnact Robo"
                );
                await new RoboRepository(db).UpsertAsync(
                    characterId,
                    new RoboData(1, roboCharacter) { OwnerAvatarId = (uint)characterId },
                    TestContext.Current.CancellationToken
                );
            }

            var state = new SharedState();
            var actor = new CapturingPlayerSession
            {
                CharacterId = (uint)characterId,
                MapId = MyRoomInfo.BaseMapId,
                MyRoomId = (uint)characterId,
            };
            actor.AccompanyingRoboIds.Add(1);
            var peer = new CapturingPlayerSession
            {
                CharacterId = 99,
                MapId = MyRoomInfo.BaseMapId,
                MyRoomId = (uint)characterId,
            };
            state.RegisterClient(ServerType.Area, actor);
            state.RegisterClient(ServerType.Area, peer);

            var handler = new AreaRoboFurnactEndHandler(
                new RoboRepository(new MainContext(options)),
                state
            );
            var writer = new PacketWriter();
            writer.Write(1u);
            await handler.HandleAsync(
                writer.ToBytes(),
                actor,
                TestContext.Current.CancellationToken
            );

            Assert.Contains(actor.Sent, packet => packet.Type == PacketType.NotifyRoboFurnactEnd);
            var notify = Assert.Single(
                peer.Sent,
                packet => packet.Type == PacketType.NotifyRoboFurnactEnd
            );
            Assert.Equal(1u, new PacketReader(notify.Payload).ReadUInt());
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task StorageClose_SendsFurnCloseWhenOpenedFromWardrobe()
    {
        var session = new CapturingPlayerSession
        {
            CharacterId = 1,
            StorageOpenContext = StorageOpenContext.Wardrobe,
        };
        await new AreaStorageCloseHandler().HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        Assert.Collection(
            session.Sent,
            packet =>
            {
                Assert.Equal(PacketType.StorageCloseResponse, packet.Type);
                Assert.Equal(0u, new PacketReader(packet.Payload).ReadUInt());
            },
            packet =>
            {
                Assert.Equal(PacketType.StorageFurnCloseResponse, packet.Type);
                Assert.Equal(0u, new PacketReader(packet.Payload).ReadUInt());
            }
        );
        Assert.Equal(StorageOpenContext.None, session.StorageOpenContext);
    }

    private static byte[] BuildFurnactStartPayload(
        uint roboId,
        uint furnitureId,
        MovementData start
    )
    {
        var writer = new PacketWriter();
        writer.Write(roboId);
        writer.Write(furnitureId);
        writer.Write(start.ToBytes());
        return writer.ToBytes();
    }
}
