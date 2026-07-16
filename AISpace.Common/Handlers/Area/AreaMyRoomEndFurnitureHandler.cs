using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaMyRoomEndFurnitureHandler(ILogger<AreaMyRoomEndFurnitureHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MyRoomEndFurnitureRequest;

    public PacketType ResponseType => PacketType.MyRoomEndFurnitureResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = MyRoomEndFurnitureRequest.FromBytes(payload.Span);

        if (!MyRoomInfo.IsMyRoomMap(session.MapId) || request.RoomId != session.CharacterId)
        {
            logger.LogWarning(
                "Rejected MyRoomEndFurniture for character {CharacterId} on map {MapId}: roomId {RoomId}",
                session.CharacterId,
                session.MapId,
                request.RoomId
            );
            await session.SendAsync(ResponseType, new MyRoomEndFurnitureResponse(1).ToBytes(), ct);
            return;
        }

        logger.LogInformation(
            "MyRoomEndFurniture for character {CharacterId} on map {MapId}",
            session.CharacterId,
            session.MapId
        );

        await session.SendAsync(ResponseType, new MyRoomEndFurnitureResponse(0).ToBytes(), ct);
    }
}
