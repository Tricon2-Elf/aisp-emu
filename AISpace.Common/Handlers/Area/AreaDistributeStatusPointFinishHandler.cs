using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public sealed class AreaDistributeStatusPointFinishHandler(IRoboRepository roboRepository, ILogger<AreaDistributeStatusPointFinishHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.DistributeStatusPointFinishRequest;
    public PacketType ResponseType => PacketType.DistributeStatusPointFinishResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = DistributeStatusPointFinishRequest.FromBytes(payload.Span);
        var updated = session.CharacterId != 0 && await roboRepository.ReplaceDistributedStatusPointsAsync(checked((int)session.CharacterId), request.RoboId, request.Values, ct);
        if (!updated)
            logger.LogWarning("Rejected distributed status-point commit for character {CharacterId}: Robo {RoboId}", session.CharacterId, request.RoboId);

        await session.SendAsync(ResponseType, new DistributeStatusPointFinishResponse(updated ? 0u : 1u, request.RoboId).ToBytes(), ct);
    }
}
