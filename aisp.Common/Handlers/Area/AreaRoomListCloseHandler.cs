using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public sealed class AreaRoomListCloseHandler(
    IMyRoomRepository myRoomRepository,
    DirectMapLinkTransitionService directMapLinkTransitionService,
    ILogger<AreaRoomListCloseHandler> logger
) : PacketHandlerBase<RoomListCloseRequest, RoomListCloseResponse>, IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.RoomListCloseRequest;
    public override PacketType ResponseType => PacketType.RoomListCloseResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<RoomListCloseResponse?> HandleAsync(
        RoomListCloseRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        // roomId 0 = cancel / close without entering.
        if (request.RoomId == 0)
            return new RoomListCloseResponse(0);

        if (request.RoomId > int.MaxValue)
            return new RoomListCloseResponse(1);

        var room = await myRoomRepository.GetRoomAsync(checked((int)request.RoomId), ct);
        if (room is null)
        {
            logger.LogWarning(
                "Room list close for character {CharacterId}: room {RoomId} not found",
                session.CharacterId,
                request.RoomId
            );
            return new RoomListCloseResponse(1);
        }

        if (!await directMapLinkTransitionService.TryTeleportToRoomAsync(session, room, ct))
        {
            logger.LogWarning(
                "Room list close for character {CharacterId}: denied or failed teleport to room {RoomId}",
                session.CharacterId,
                request.RoomId
            );
            return new RoomListCloseResponse(1);
        }

        return new RoomListCloseResponse(0);
    }
}
