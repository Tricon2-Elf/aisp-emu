using aisp.Common.Handlers.Area;
using aisp.Common.Tests.Support;
using aisp.Network;

namespace aisp.Common.Tests;

public class AreaGetAdventureWorkListHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsSuccessfulEmptyWorkList()
    {
        var handler = new AreaGetAdventureWorkListHandler();
        var session = new CapturingPlayerSession { CharacterId = 1 };

        await handler.HandleAsync(
            ReadOnlyMemory<byte>.Empty,
            session,
            TestContext.Current.CancellationToken
        );

        var sent = Assert.Single(session.Sent);
        Assert.Equal(PacketType.GetAdventureWorkListResponse, sent.Type);

        var reader = new PacketReader(sent.Payload);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(12, sent.Payload.Length);
    }
}
