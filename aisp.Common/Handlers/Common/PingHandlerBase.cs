using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Common;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Common;

public abstract class PingHandlerBase(ILogger logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.Ping;
    public PacketType ResponseType => PacketType.Ping;
    public abstract ServerType ServerType { get; }

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        logger.LogTrace("Ping from {ConnectionId}", session.ConnectionId);
        var ping = PingRequest.FromBytes(payload.Span);
        await session.SendAsync(PacketType.Ping, new PingResponse(ping.Time).ToBytes(), ct);
    }
}
