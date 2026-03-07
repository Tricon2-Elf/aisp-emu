using AISpace.Network;
using AISpace.Network.Packets.Common;

namespace AISpace.Common.Handlers.Common;

public abstract class PingHandlerBase(ILogger logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.Ping;
    public PacketType ResponseType => PacketType.Ping;
    public abstract MessageDomain Domain { get; }

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        logger.LogTrace("Ping from {ConnectionId}", connection.Id);
        var ping = PingRequest.FromBytes(payload.Span);
        await connection.SendAsync(PacketType.Ping, ping.ToBytes(), ct);
    }
}

public class AuthPingHandler(ILogger<AuthPingHandler> logger) : PingHandlerBase(logger)
{
    public override MessageDomain Domain => MessageDomain.Auth;
}

public class MsgPingHandler(ILogger<MsgPingHandler> logger) : PingHandlerBase(logger)
{
    public override MessageDomain Domain => MessageDomain.Msg;
}

public class AreaPingHandler(ILogger<AreaPingHandler> logger) : PingHandlerBase(logger)
{
    public override MessageDomain Domain => MessageDomain.Area;
}
