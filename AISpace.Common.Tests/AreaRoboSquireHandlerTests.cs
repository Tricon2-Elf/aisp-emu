using AISpace.Common.DAL;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Data;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISpace.Common.Tests;

public class AreaRoboSquireHandlerTests
{
    [Fact]
    public async Task HandleAsync_MarksOwnedRoboAsAccompanyingOnlyForCurrentSession()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);
            var objectId = RoboRepository.GetObjectId(1, 1);
            var chara = new CharaData(objectId, 1002011, "Robot") { Visual = new CharaVisual(BloodType.A, 1, 1, 0, objectId, 0, 10930010) };
            await using (var seedDb = new MainContext(options))
                await new RoboRepository(seedDb).UpsertAsync(1, new RoboData(1, chara, (uint)RoboState.InMyRoom) { OwnerAvatarId = 1 }, TestContext.Current.CancellationToken);

            await using (var handlerDb = new MainContext(options))
            {
                var state = new SharedState();
                var session = new CapturingPlayerSession
                {
                    CharacterId = 1,
                    ChannelId = 4,
                    MapId = 40000001,
                    X = 11,
                    Y = 22,
                    Z = 33,
                    Rotation = 180,
                };
                var peer = new CapturingPlayerSession
                {
                    CharacterId = 2,
                    ChannelId = 4,
                    MapId = 40000001,
                };
                state.RegisterClient(ServerType.Area, session);
                state.RegisterClient(ServerType.Area, peer);
                var handler = new AreaRoboSquireHandler(new RoboRepository(handlerDb), NullLogger<AreaRoboSquireHandler>.Instance, state);
                var writer = new PacketWriter();
                writer.Write(1u);

                await handler.HandleAsync(writer.ToBytes(), session, TestContext.Current.CancellationToken);

                Assert.Contains(1u, session.AccompanyingRoboIds);
                Assert.Collection(
                    session.Sent,
                    sent =>
                    {
                        Assert.Equal(PacketType.NotifyUpdateRoboState, sent.Type);
                        var reader = new PacketReader(sent.Payload);
                        Assert.Equal(1u, reader.ReadUInt());
                        Assert.Equal(objectId, reader.ReadUInt());
                        Assert.Equal((uint)RoboState.Accompanying, reader.ReadUInt());
                        var map = CharacterMapData.FromBytes(reader.ReadBytes(CharacterMapData.WireSize));
                        Assert.Equal(4u, map.ChannelId);
                        Assert.Equal(40000001u, map.MapId);
                        Assert.Equal(11f, map.Movement.X);
                        Assert.Equal(22f, map.Movement.Y);
                        Assert.Equal(33f, map.Movement.Z);
                        Assert.Equal(180, map.Movement.Rotation);
                    },
                    sent =>
                    {
                        Assert.Equal(PacketType.RoboSquireResponse, sent.Type);
                        var reader = new PacketReader(sent.Payload);
                        Assert.Equal(1u, reader.ReadUInt());
                        Assert.Equal(0u, reader.ReadUInt());
                    }
                );

                var peerNotify = Assert.Single(peer.Sent);
                Assert.Equal(PacketType.NotifyRoboData, peerNotify.Type);
                var notifyReader = new PacketReader(peerNotify.Payload);
                Assert.Equal(0u, notifyReader.ReadUInt());
                var remote = RoboData.FromBytes(notifyReader.ReadBytes(RoboData.WireSize));
                Assert.Equal(1u, remote.RoboId);
                Assert.Equal(1u, remote.OwnerAvatarId);
                Assert.Equal(objectId, remote.Character.SlotId);
            }

            await using var verifyDb = new MainContext(options);
            var stored = await new RoboRepository(verifyDb).GetAsync(1, 1, TestContext.Current.CancellationToken);
            Assert.NotNull(stored);
            Assert.Equal((uint)RoboState.InMyRoom, stored.State);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task HandleAsync_RejectsUnownedRobo()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);
            await using var handlerDb = new MainContext(options);
            var handler = new AreaRoboSquireHandler(new RoboRepository(handlerDb), NullLogger<AreaRoboSquireHandler>.Instance);
            var session = new CapturingPlayerSession { CharacterId = 1 };
            var writer = new PacketWriter();
            writer.Write(99u);

            await handler.HandleAsync(writer.ToBytes(), session, TestContext.Current.CancellationToken);

            var sent = Assert.Single(session.Sent);
            Assert.Equal(PacketType.RoboSquireResponse, sent.Type);
            var reader = new PacketReader(sent.Payload);
            Assert.Equal(99u, reader.ReadUInt());
            Assert.Equal(1u, reader.ReadUInt());
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
