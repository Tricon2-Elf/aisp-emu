using AISpace.Common.Handlers.Area;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging.Abstractions;

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
    public async Task MoveRobo_ParsesWithoutResponse()
    {
        var handler = new AreaMoveRoboHandler();
        var session = new CapturingPlayerSession { CharacterId = 1 };
        // Payload from client log
        byte[] payload =
        [
            0x01,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0xF6,
            0x42,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x2A,
            0xC3,
            0x51,
            0x04,
            0x00,
            0x00,
            0xF6,
            0x42,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x2A,
            0xC3,
            0x51,
            0x04,
        ];

        await handler.HandleAsync(payload, session, TestContext.Current.CancellationToken);
        Assert.Empty(session.Sent);
    }

    [Fact]
    public async Task RoboAiscriptStart_RejectsUntilImplemented()
    {
        var handler = new AreaRoboAiscriptStartHandler(NullLogger<AreaRoboAiscriptStartHandler>.Instance);
        var session = new CapturingPlayerSession { CharacterId = 1 };
        var writer = new PacketWriter();
        writer.Write(1u);

        await handler.HandleAsync(writer.ToBytes(), session, TestContext.Current.CancellationToken);

        var sent = Assert.Single(session.Sent);
        Assert.Equal(PacketType.RoboAiscriptStartResponse, sent.Type);
        var reader = new PacketReader(sent.Payload);
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt()); // failure — avoids empty start/end retry loop
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
