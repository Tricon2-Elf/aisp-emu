using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public sealed class AreaDistributeStatusPointAddHandler(
    IRoboRepository roboRepository,
    ILogger<AreaDistributeStatusPointAddHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.DistributeStatusPointAddRequest;
    public PacketType ResponseType => PacketType.DistributeStatusPointAddResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = DistributeStatusPointAddRequest.FromBytes(payload.Span);
        var owned =
            session.CharacterId != 0
            && request.Type < RoboData.DistributedStatusPointCount
            && await roboRepository.ExistsAsync(
                checked((int)session.CharacterId),
                request.RoboId,
                ct
            );
        if (!owned)
            logger.LogWarning(
                "Rejected distributed status-point preview for character {CharacterId}: Robo {RoboId}, type {Type}",
                session.CharacterId,
                request.RoboId,
                request.Type
            );

        var result = owned ? 0u : 1u;
        var cost = owned ? request.Value : 0u;
        await session.SendAsync(
            ResponseType,
            new DistributeStatusPointAddResponse(
                result,
                request.RoboId,
                request.Type,
                cost
            ).ToBytes(),
            ct
        );
    }
}
