using AISpace.Common.Handlers.Area;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Tests;

public class AreaGetRoboCreateInfoHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsDefaultModelHairstyleAndEquips()
    {
        var handler = new AreaGetRoboCreateInfoHandler();
        var session = new CapturingPlayerSession { CharacterId = 1 };

        await handler.HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        var sent = Assert.Single(session.Sent);
        Assert.Equal(PacketType.GetRoboCreateInfoResponse, sent.Type);

        var reader = new PacketReader(sent.Payload);
        Assert.Equal(1002011u, reader.ReadUInt());
        Assert.Equal(10930010u, reader.ReadUInt());
        Assert.Equal(4u, reader.ReadUInt());
        Assert.Equal(10100060u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(10200090u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(10400000u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(10500010u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(44, sent.Payload.Length);
    }

    [Fact]
    public void GetRoboCreateInfoResponse_MatchesClientMaxEquipBuffer()
    {
        // Client alloc is 252 bytes = 3×uint + 30×(itemId+socket).
        var equips = Enumerable
            .Range(0, 30)
            .Select(i => new AISpace.Network.Data.ItemSlotInfo((uint)(1000 + i), 0))
            .ToList();
        var bytes = new GetRoboCreateInfoResponse(1, 2, equips).ToBytes();
        Assert.Equal(252, bytes.Length);
    }
}
