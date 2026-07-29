using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaMyRoomUseFurnitureHandler(ILogger<AreaMyRoomUseFurnitureHandler> logger)
    : IPacketHandler,
        IRequiresAuthenticatedSession
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

        await session.SendAsync(ResponseType, new MyRoomUseFurnitureResponse(0).ToBytes(), ct);
        await session.SendAsync(
            PacketType.NotifyMyRoomUseFurniture,
            new NotifyMyRoomUseFurniture(request.RoomId, request.FurnId).ToBytes(),
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
