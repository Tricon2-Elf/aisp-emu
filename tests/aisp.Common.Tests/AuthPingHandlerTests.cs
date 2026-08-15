using aisp.Common.Handlers.Common;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Packets.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace aisp.Common.Tests;

public class AuthPingHandlerTests
{
    [Fact]
    public async Task EchoesPingPayload()
    {
        var handler = new AuthPingHandler(NullLogger<AuthPingHandler>.Instance);
        var session = new CapturingPlayerSession();
        var w = new PacketWriter();
        w.Write(0x11223344u);
        var payload = w.ToBytes();

        await handler.HandleAsync(payload, session, TestContext.Current.CancellationToken);

        Assert.Single(session.Sent);
        Assert.Equal(PacketType.Ping, session.Sent[0].Type);
        Assert.Equal(payload, session.Sent[0].Payload);
    }
}
