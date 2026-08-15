using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public class AreaMyRoomUseFurnitureHandler(
    IMyRoomRepository myRoomRepository,
    SharedState state,
    ILogger<AreaMyRoomUseFurnitureHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MyRoomUseFurnitureRequest;

    public PacketType ResponseType => PacketType.MyRoomUseFurnitureResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = MyRoomUseFurnitureRequest.FromBytes(payload.Span);

        if (
            !MyRoomInfo.IsMyRoomMap(session.MapId)
            || session.MyRoomId == 0
            || request.RoomId != session.MyRoomId
        )
        {
            logger.LogWarning(
                "Rejected MyRoomUseFurniture for character {CharacterId} on map {MapId}: roomId {RoomId} furnId {FurnId}",
                session.CharacterId,
                session.MapId,
                request.RoomId,
                request.FurnId
            );
            await session.SendAsync(ResponseType, new MyRoomUseFurnitureResponse(1).ToBytes(), ct);
            return;
        }

        var furniture = await myRoomRepository.GetFurnitureAsync(
            checked((int)request.RoomId),
            request.FurnId,
            ct
        );
        if (furniture is null)
        {
            logger.LogWarning(
                "Rejected MyRoomUseFurniture for character {CharacterId}: furniture {FurnId} missing in room {RoomId}",
                session.CharacterId,
                request.FurnId,
                request.RoomId
            );
            await session.SendAsync(ResponseType, new MyRoomUseFurnitureResponse(1).ToBytes(), ct);
            return;
        }

        await session.SendAsync(ResponseType, new MyRoomUseFurnitureResponse(0).ToBytes(), ct);
        await MyRoomFurnitureNotification.BroadcastToRoomAsync(
            state,
            session,
            request.RoomId,
            PacketType.NotifyMyRoomUseFurniture,
            new NotifyMyRoomUseFurniture(request.RoomId, request.FurnId).ToBytes(),
            includeSource: true,
            ct
        );

        logger.LogInformation(
            "MyRoomUseFurniture ack for character {CharacterId} furnId {FurnId} reason {Reason}",
            session.CharacterId,
            request.FurnId,
            request.Reason
        );
    }
}
