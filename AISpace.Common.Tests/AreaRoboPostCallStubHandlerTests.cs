using AISpace.Common.DAL;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Tests;

public class AreaRoboPostCallStubHandlerTests
{
    [Fact]
    public async Task GetAiPaletteList_ReturnsFixed296BytePayload()
    {
        var handler = new AreaGetAiPaletteListHandler();
        var session = new CapturingPlayerSession { CharacterId = 1 };
        var writer = new PacketWriter();
        writer.Write(1u);

        await handler.HandleAsync(writer.ToBytes(), session, TestContext.Current.CancellationToken);

        var sent = Assert.Single(session.Sent);
        Assert.Equal(PacketType.GetAiPaletteListResponse, sent.Type);
        Assert.Equal(296, sent.Payload.Length);
        var reader = new PacketReader(sent.Payload);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
    }

    [Fact]
    public async Task GetCosplayList_ReturnsEmptySuccess()
    {
        var handler = new AreaGetCosplayListHandler();
        var session = new CapturingPlayerSession { CharacterId = 1 };
        var writer = new PacketWriter();
        writer.Write(1u);

        await handler.HandleAsync(writer.ToBytes(), session, TestContext.Current.CancellationToken);

        var sent = Assert.Single(session.Sent);
        Assert.Equal(PacketType.GetCosplayListResponse, sent.Type);
        Assert.Equal(0x13CF, (int)sent.Type);
        var reader = new PacketReader(sent.Payload);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
    }

    [Fact]
    public async Task MoveRobo_BroadcastsMovementToAreaPeers()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        try
        {
            await TestDb.SeedCharacterAsync(options, 1, TestContext.Current.CancellationToken);
            await using (var seedDb = new MainContext(options))
            {
                var objectId = RoboRepository.GetObjectId(1, 1);
                var robo = new RoboData(1, new CharaData(objectId, 1_002_011, "Moving Robo"), (uint)RoboState.Accompanying) { OwnerAvatarId = 1 };
                await new RoboRepository(seedDb).UpsertAsync(1, robo, TestContext.Current.CancellationToken);
            }

            var state = new SharedState();
            var session = new CapturingPlayerSession
            {
                CharacterId = 1,
                MapId = 40990200,
                ChannelId = 1,
            };
            var peer = new CapturingPlayerSession
            {
                CharacterId = 2,
                MapId = 40990200,
                ChannelId = 1,
            };
            state.RegisterClient(ServerType.Area, session);
            state.RegisterClient(ServerType.Area, peer);
            session.AccompanyingRoboIds.Add(1);

            await using var handlerDb = new MainContext(options);
            var handler = new AreaMoveRoboHandler(new RoboRepository(handlerDb), state);
            var payloadWriter = new PacketWriter();
            payloadWriter.Write(1u);
            payloadWriter.Write(new MovementData(123, 0, -170, 180, MovementType.Running).ToBytes());
            payloadWriter.Write(new MovementData(124, 0, -171, 180, MovementType.Running).ToBytes());

            await handler.HandleAsync(payloadWriter.ToBytes(), session, TestContext.Current.CancellationToken);

            Assert.Empty(session.Sent);
            var sent = Assert.Single(peer.Sent);
            Assert.Equal(PacketType.AvatarNotifyMove, sent.Type);
            var reader = new PacketReader(sent.Payload);
            Assert.Equal(2u, reader.ReadUInt());
            Assert.Equal(RoboRepository.GetObjectId(1, 1), reader.ReadUInt());
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task RoboAiscriptEnd_ParsesWithoutResponse()
    {
        var handler = new AreaRoboAiscriptEndHandler();
        var session = new CapturingPlayerSession { CharacterId = 1 };
        var writer = new PacketWriter();
        writer.Write(1u);

        await handler.HandleAsync(writer.ToBytes(), session, TestContext.Current.CancellationToken);
        Assert.Empty(session.Sent);
    }
}
