using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public sealed class AreaRoboAttachRequestRHandler(
    IRoboRepository roboRepository,
    ILogger<AreaRoboAttachRequestRHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.RoboAttachRequestRRequest;
    public PacketType ResponseType => PacketType.RoboAttachResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = RoboAttachRequestRRequest.FromBytes(payload.Span);
        var owned =
            session.CharacterId != 0
            && await roboRepository.ExistsAsync(
                checked((int)session.CharacterId),
                request.RoboId,
                ct
            );
        var result = owned ? request.Result : 1u;

        if (!owned)
            logger.LogWarning(
                "Rejected Robo attach reply for character {CharacterId}: Robo {RoboId} is not owned by the character",
                session.CharacterId,
                request.RoboId
            );
        else
            logger.LogDebug(
                "Completing Robo conversation handshake for character {CharacterId}: Robo {RoboId}, result {Result}",
                session.CharacterId,
                request.RoboId,
                result
            );

        await session.SendAsync(
            ResponseType,
            new RoboAttachResponse(request.RoboId, result).ToBytes(),
            ct
        );
    }
}
