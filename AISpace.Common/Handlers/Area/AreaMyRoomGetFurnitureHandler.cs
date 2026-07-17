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

        // Built-in room objects: the retail client expects the door and closet/wardrobe to arrive
        // as furniture notifies (they are not part of the map geometry). The owner id must match
        // the OwnerId sent in NotifyChangeMyRoom (we use the character id for both).
        var ownerId = session.CharacterId;
        var stage = MyRoomInfo.GetRoomStage(session.MapId);
        var (doorX, doorZ) = MyRoomInfo.GetEntrancePosition(stage);
        var (closetX, closetZ) = MyRoomInfo.GetClosetPosition(stage);

        var door = new MyRoomNotifyFurniture(ownerId, MyRoomInfo.DoorSerialId, MyRoomInfo.ActionDoor, MyRoomInfo.DoorItemId, doorX, 0f, doorZ);
        // Wire ActionType is ignored for click routing; ItemId must have furniture.csv アクション=4.
        var closet = new MyRoomNotifyFurniture(ownerId, MyRoomInfo.ClosetSerialId, MyRoomInfo.ActionUseFurniture, MyRoomInfo.ClosetItemId, closetX, 0f, closetZ);

        await session.SendAsync(PacketType.MyRoomNotifyFurniture, door.ToBytes(), ct);
        await session.SendAsync(PacketType.MyRoomNotifyFurniture, closet.ToBytes(), ct);

        logger.LogInformation("Sent MyRoom door/closet furniture to character {CharacterId} on map {MapId} (stage {Stage}: door at ({DoorX}, {DoorZ}), closet at ({ClosetX}, {ClosetZ}))", ownerId, session.MapId, stage, doorX, doorZ, closetX, closetZ);
    }
}
