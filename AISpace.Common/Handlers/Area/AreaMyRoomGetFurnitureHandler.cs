using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaMyRoomGetFurnitureHandler(ILogger<AreaMyRoomGetFurnitureHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MyRoomGetFurnitureRequest;

    public PacketType ResponseType => PacketType.MyRoomGetFurnitureResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var response = new MyRoomGetFurnitureResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);

        if (!MyRoomInfo.IsMyRoomMap(session.MapId))
            return;

        var ownerId = session.CharacterId;
        var stage = MyRoomInfo.GetRoomStage(session.MapId);
        var (closetX, closetZ) = MyRoomInfo.GetClosetPosition(stage);

        var closet = new MyRoomNotifyFurniture(ownerId, MyRoomInfo.ClosetSerialId, MyRoomInfo.ActionCloset, MyRoomInfo.ClosetItemId, closetX, 0f, closetZ);
        await session.SendAsync(PacketType.MyRoomNotifyFurniture, closet.ToBytes(), ct);

        logger.LogInformation("Sent MyRoom closet furniture to character {CharacterId} on map {MapId} (stage {Stage}: {ClosetItemId} at ({ClosetX}, {ClosetZ}))", ownerId, session.MapId, stage, MyRoomInfo.ClosetItemId, closetX, closetZ);
    }
}
