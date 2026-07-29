using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public sealed class AreaRoboTalkPostHandler(
    IRoboRepository roboRepository,
    ILogger<AreaRoboTalkPostHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.RoboTalkPostRequest;
    public PacketType ResponseType => PacketType.RoboTalkForwardNotify;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = RoboTalkPostRequest.FromBytes(payload.Span);
        var owned =
            session.CharacterId != 0
            && await roboRepository.ExistsAsync(
                checked((int)session.CharacterId),
                request.RoboId,
                ct
            );
        if (!owned)
        {
            logger.LogWarning(
                "Rejected Robo conversation message for character {CharacterId}: Robo {RoboId} is not owned by the character",
                session.CharacterId,
                request.RoboId
            );
            return;
        }

        logger.LogDebug(
            "Forwarding Robo conversation message for character {CharacterId}: Robo {RoboId}",
            session.CharacterId,
            request.RoboId
        );
        await session.SendAsync(
            ResponseType,
            new RoboTalkForwardNotify(request.RoboId, request.Message).ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.RoboGrantNextMessageNoticeNotify,
            new RoboGrantNextMessageNoticeNotify(request.RoboId).ToBytes(),
            ct
        );
    }
}
