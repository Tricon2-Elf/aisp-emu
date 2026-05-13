using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Common;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Common;

public abstract class PingHandlerBase(ILogger logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.Ping;
    public PacketType ResponseType => PacketType.Ping;
    public abstract ServerType ServerType { get; }

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        logger.LogTrace("Ping from {ConnectionId}", session.ConnectionId);
        var ping = PingRequest.FromBytes(payload.Span);
        await session.SendAsync(PacketType.Ping, new PingResponse(ping.Time).ToBytes(), ct);
    }
}

public class AuthPingHandler(ILogger<AuthPingHandler> logger) : PingHandlerBase(logger)
{
    public override ServerType ServerType => ServerType.Auth;
}

public class MsgPingHandler(ILogger<MsgPingHandler> logger) : PingHandlerBase(logger)
{
    public override ServerType ServerType => ServerType.Msg;
}

public class AreaPingHandler(ILogger<AreaPingHandler> logger) : PingHandlerBase(logger)
{
    public override ServerType ServerType => ServerType.Area;
}
