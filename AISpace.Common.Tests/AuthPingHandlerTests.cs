using AISpace.Common.Handlers.Common;
using AISpace.Common.Tests.Support;
using AISpace.Network;
using AISpace.Network.Packets.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AISpace.Common.Tests;

public class AuthPingHandlerTests
{
    [Fact]
    public async Task EchoesPingPayload()
    {
        var handler = new AuthPingHandler(NullLogger<AuthPingHandler>.Instance);
        var session = new CapturingPlayerSession();
        var ping = new PingRequest(0x11223344);

        await handler.HandleAsync(ping.ToBytes(), session, TestContext.Current.CancellationToken);

        Assert.Single(session.Sent);
        Assert.Equal(PacketType.Ping, session.Sent[0].Type);
        Assert.Equal(ping.ToBytes(), session.Sent[0].Payload);
    }
}
