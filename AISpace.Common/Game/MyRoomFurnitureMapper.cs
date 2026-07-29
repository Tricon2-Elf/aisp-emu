using AISpace.Common.DAL.Entities;
using AISpace.Network.Data;

namespace AISpace.Common.Game;

internal static class MyRoomFurnitureMapper
{
    public static MyRoomFurnitureData ToPacket(MyRoomFurniture furniture) =>
        new(
            checked((uint)furniture.RoomId),
            furniture.FurnitureId,
            PlacementState: 0,
            checked((uint)furniture.ItemId),
            furniture.PositionX,
            furniture.PositionY,
            furniture.PositionZ,
            furniture.DirectionX,
            furniture.DirectionY,
            Active: 1
        );
}
