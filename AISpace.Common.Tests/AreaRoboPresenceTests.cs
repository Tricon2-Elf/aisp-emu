using AISpace.Common.DAL;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISpace.Common.Tests;

public class AreaRoboPresenceTests
{
    [Fact]
    public async Task SynchronizePeers_SynchronizesAccompanyingRobosInBothDirections()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);
            await TestDb.SeedCharacterAsync(options, 2, TestContext.Current.CancellationToken);

            await using (var seedDb = new MainContext(options))
            {
                var objectId = RoboRepository.GetObjectId(1, 1);
                var robo = new RoboData(
                    1,
                    new CharaData(objectId, 1002011, "Following Robo"),
                    (uint)RoboState.Accompanying
                )
                {
                    OwnerAvatarId = 1,
                };
                await new RoboRepository(seedDb).UpsertAsync(
                    1,
                    robo,
                    TestContext.Current.CancellationToken
                );

                var peerObjectId = RoboRepository.GetObjectId(2, 1);
                var peerRobo = new RoboData(
                    1,
                    new CharaData(peerObjectId, 1002011, "Peer Robo"),
                    (uint)RoboState.Accompanying
                )
                {
                    OwnerAvatarId = 2,
                };
                await new RoboRepository(seedDb).UpsertAsync(
                    2,
                    peerRobo,
                    TestContext.Current.CancellationToken
                );
            }

            AISpace.Common.DAL.Entities.Character ownerCharacter;
            AISpace.Common.DAL.Entities.Character peerCharacter;
            await using (var characterDb = new MainContext(options))
            {
                ownerCharacter = await characterDb
                    .Characters.AsNoTracking()
                    .SingleAsync(x => x.Id == 1, TestContext.Current.CancellationToken);
                peerCharacter = await characterDb
                    .Characters.AsNoTracking()
                    .SingleAsync(x => x.Id == 2, TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var owner = new CapturingPlayerSession
            {
                CharacterId = 1,
                Character = ownerCharacter,
                MapId = 40990200,
                ChannelId = 1,
                X = 10,
                Y = 20,
                Z = 30,
                Rotation = 180,
            };
            var peer = new CapturingPlayerSession
            {
                CharacterId = 2,
                Character = peerCharacter,
                MapId = 40990200,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, owner);
            state.RegisterClient(ServerType.Area, peer);
            owner.AccompanyingRoboIds.Add(1);
            peer.AccompanyingRoboIds.Add(1);

            await using var handlerDb = new MainContext(options);
            await AreaAvatarPresenceSync.SynchronizePeersAsync(
                state,
                owner,
                NullLogger.Instance,
                new RoboRepository(handlerDb),
                myRoomRepository: null,
                TestContext.Current.CancellationToken
            );

            Assert.Collection(
                peer.Sent,
                sent => Assert.Equal(PacketType.AvatarNotifyData, sent.Type),
                sent =>
                {
                    Assert.Equal(PacketType.NotifyRoboData, sent.Type);
                    var reader = new PacketReader(sent.Payload);
                    Assert.Equal(0u, reader.ReadUInt());
                    var remote = RoboData.FromBytes(reader.ReadBytes(RoboData.WireSize));
                    Assert.Equal(1u, remote.RoboId);
                    Assert.Equal(1u, remote.OwnerAvatarId);
                    Assert.Equal(RoboRepository.GetObjectId(1, 1), remote.Character.SlotId);
                    Assert.Equal((uint)RoboState.Accompanying, remote.State);
                }
            );

            Assert.Collection(
                owner.Sent,
                sent => Assert.Equal(PacketType.AvatarNotifyData, sent.Type),
                sent =>
                {
                    Assert.Equal(PacketType.NotifyRoboData, sent.Type);
                    var reader = new PacketReader(sent.Payload);
                    Assert.Equal(0u, reader.ReadUInt());
                    var remote = RoboData.FromBytes(reader.ReadBytes(RoboData.WireSize));
                    Assert.Equal(1u, remote.RoboId);
                    Assert.Equal(2u, remote.OwnerAvatarId);
                }
            );
            Assert.Contains(RoboRepository.GetObjectId(1, 1), peer.VisibleRemoteRoboObjectIds);
            Assert.Contains(RoboRepository.GetObjectId(2, 1), owner.VisibleRemoteRoboObjectIds);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task SynchronizePeers_SpawnsMyRoomOwnerRoboForVisitorAtMapEnter()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 42, TestContext.Current.CancellationToken);
            await TestDb.SeedCharacterAsync(options, 99, TestContext.Current.CancellationToken);

            var objectId = RoboRepository.GetObjectId(42, 1);
            await using (var seedDb = new MainContext(options))
            {
                await new RoboRepository(seedDb).UpsertAsync(
                    42,
                    new RoboData(1, new CharaData(objectId, 1_002_011, "Room Robo"))
                    {
                        OwnerAvatarId = 42,
                    },
                    TestContext.Current.CancellationToken
                );
            }

            AISpace.Common.DAL.Entities.Character visitorCharacter;
            await using (var characterDb = new MainContext(options))
            {
                visitorCharacter = await characterDb
                    .Characters.AsNoTracking()
                    .SingleAsync(x => x.Id == 99, TestContext.Current.CancellationToken);
            }

            var visitor = new CapturingPlayerSession
            {
                CharacterId = 99,
                Character = visitorCharacter,
                MapId = MyRoomInfo.TwelveTatamiMapId,
                MyRoomId = 42,
                ChannelId = 1,
                X = 10,
                Y = 0,
                Z = -20,
            };

            var state = new SharedState();
            state.RememberRoboMovement(
                42,
                1,
                new MovementData(173f, 0f, -220f, 180, MovementType.Stopped)
            );

            await using var handlerDb = new MainContext(options);
            await AreaAvatarPresenceSync.SynchronizePeersAsync(
                state,
                visitor,
                NullLogger.Instance,
                new RoboRepository(handlerDb),
                new MyRoomRepository(handlerDb),
                TestContext.Current.CancellationToken
            );

            var spawn = Assert.Single(
                visitor.Sent,
                packet => packet.Type == PacketType.NotifyRoboData
            );
            var reader = new PacketReader(spawn.Payload);
            Assert.Equal(0u, reader.ReadUInt());
            var remote = RoboData.FromBytes(reader.ReadBytes(RoboData.WireSize));
            Assert.Equal(1u, remote.RoboId);
            Assert.Equal(42u, remote.OwnerAvatarId);
            Assert.Equal((uint)RoboState.Accompanying, remote.State);
            Assert.Equal(objectId, remote.Character.SlotId);
            Assert.Equal(173f, remote.Character.Map.Movement.X);
            Assert.Equal(-220f, remote.Character.Map.Movement.Z);
            Assert.Equal(180, remote.Character.Map.Movement.Rotation);
            Assert.Contains(objectId, visitor.VisibleRemoteRoboObjectIds);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
