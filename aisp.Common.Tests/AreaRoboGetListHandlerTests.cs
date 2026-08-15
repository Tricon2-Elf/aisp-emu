using aisp.Common.DAL;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Data;

namespace aisp.Common.Tests;

public class AreaRoboGetListHandlerTests
{
    [Fact]
    public async Task HandleAsync_DoesNotRestoreAccompanimentFromDatabase()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 9, TestContext.Current.CancellationToken);
            var objectId = RoboRepository.GetObjectId(9, 1);
            var robo = new RoboData(
                1,
                new CharaData(objectId, 1002011, "Database Robo"),
                (uint)RoboState.Accompanying
            )
            {
                OwnerAvatarId = 9,
            };

            await using (var writeDb = new MainContext(options))
                await new RoboRepository(writeDb).UpsertAsync(
                    9,
                    robo,
                    TestContext.Current.CancellationToken
                );

            await using var readDb = new MainContext(options);
            var handler = new AreaRoboGetListHandler(new RoboRepository(readDb));
            var session = new CapturingPlayerSession
            {
                CharacterId = 9,
                ChannelId = 1,
                MapId = 40990200,
                X = 100,
                Y = 20,
                Z = 300,
                Rotation = 180,
            };

            await handler.HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                session,
                TestContext.Current.CancellationToken
            );

            var sent = Assert.Single(session.Sent);
            Assert.Equal(PacketType.RoboGetListResponse, sent.Type);
            var reader = new PacketReader(sent.Payload);
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(1u, reader.ReadUInt());
            var loaded = RoboData.FromBytes(reader.ReadBytes(RoboData.WireSize));
            Assert.Equal(9u, loaded.OwnerAvatarId);
            Assert.Equal("Database Robo", loaded.Character.Name);
            Assert.Equal((uint)RoboState.InMyRoom, loaded.State);
            Assert.Equal(0u, loaded.Character.Map.ChannelId);
            Assert.Equal(0u, loaded.Character.Map.MapId);
            Assert.Empty(session.AccompanyingRoboIds);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task HandleAsync_UsesCurrentMapForRoboAccompanyingInCurrentSession()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 9, TestContext.Current.CancellationToken);
            var objectId = RoboRepository.GetObjectId(9, 1);
            var robo = new RoboData(
                1,
                new CharaData(objectId, 1002011, "Following Robo"),
                (uint)RoboState.InMyRoom
            )
            {
                OwnerAvatarId = 9,
            };

            await using (var writeDb = new MainContext(options))
                await new RoboRepository(writeDb).UpsertAsync(
                    9,
                    robo,
                    TestContext.Current.CancellationToken
                );

            await using var readDb = new MainContext(options);
            var handler = new AreaRoboGetListHandler(new RoboRepository(readDb));
            var session = new CapturingPlayerSession
            {
                CharacterId = 9,
                ChannelId = 2,
                MapId = 40990200,
                X = 100,
                Y = 20,
                Z = 300,
                Rotation = 180,
            };
            session.AccompanyingRoboIds.Add(1);

            await handler.HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                session,
                TestContext.Current.CancellationToken
            );

            var sent = Assert.Single(session.Sent);
            var reader = new PacketReader(sent.Payload);
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(1u, reader.ReadUInt());
            var loaded = RoboData.FromBytes(reader.ReadBytes(RoboData.WireSize));
            Assert.Equal((uint)RoboState.Accompanying, loaded.State);
            Assert.Equal(2u, loaded.Character.Map.ChannelId);
            Assert.Equal(40990200u, loaded.Character.Map.MapId);
            Assert.Equal(100f, loaded.Character.Movement.X);
            Assert.Equal(20f, loaded.Character.Movement.Y);
            Assert.Equal(300f, loaded.Character.Movement.Z);
            Assert.Equal(180, loaded.Character.Movement.Rotation);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task HandleAsync_ReturnsOnlyOwnedRobosWhenAreaPeerHasAccompanyingRobo()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 9, TestContext.Current.CancellationToken);
            await TestDb.SeedCharacterAsync(options, 10, TestContext.Current.CancellationToken);
            await using (var writeDb = new MainContext(options))
            {
                var ownObjectId = RoboRepository.GetObjectId(9, 1);
                var ownRobo = new RoboData(
                    1,
                    new CharaData(ownObjectId, 1002011, "Own Robo"),
                    (uint)RoboState.Resting
                )
                {
                    OwnerAvatarId = 9,
                };
                await new RoboRepository(writeDb).UpsertAsync(
                    9,
                    ownRobo,
                    TestContext.Current.CancellationToken
                );

                var peerObjectId = RoboRepository.GetObjectId(10, 1);
                var peerRobo = new RoboData(
                    1,
                    new CharaData(peerObjectId, 1002011, "Peer Robo"),
                    (uint)RoboState.Accompanying
                )
                {
                    OwnerAvatarId = 10,
                };
                await new RoboRepository(writeDb).UpsertAsync(
                    10,
                    peerRobo,
                    TestContext.Current.CancellationToken
                );
            }

            var state = new SharedState();
            var session = new CapturingPlayerSession
            {
                CharacterId = 9,
                MapId = 40990200,
                ChannelId = 1,
            };
            var peer = new CapturingPlayerSession
            {
                CharacterId = 10,
                MapId = 40990200,
                ChannelId = 1,
                X = 100,
                Y = 20,
                Z = 300,
                Rotation = 180,
            };
            state.RegisterClient(ServerType.Area, session);
            state.RegisterClient(ServerType.Area, peer);
            peer.AccompanyingRoboIds.Add(1);

            await using var readDb = new MainContext(options);
            var handler = new AreaRoboGetListHandler(new RoboRepository(readDb));
            await handler.HandleAsync(
                ReadOnlyMemory<byte>.Empty,
                session,
                TestContext.Current.CancellationToken
            );

            var sent = Assert.Single(session.Sent);
            var reader = new PacketReader(sent.Payload);
            Assert.Equal(0u, reader.ReadUInt());
            Assert.Equal(1u, reader.ReadUInt());

            var own = RoboData.FromBytes(reader.ReadBytes(RoboData.WireSize));
            Assert.Equal(1u, own.RoboId);
            Assert.Equal(9u, own.OwnerAvatarId);
            Assert.Empty(session.VisibleRemoteRoboObjectIds);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
