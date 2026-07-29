using AISpace.Network.Data;

namespace AISpace.Network.Packets.Area;

/// <summary>recv_notify_myroom_update_furniture (0xCEAB).</summary>
public sealed class NotifyMyRoomUpdateFurniture(
    uint roomId,
    uint furnitureId,
    MyRoomFurnitureTransform transform
) : IOutgoingPacket
{
    public uint RoomId { get; } = roomId;
    public uint FurnitureId { get; } = furnitureId;
    public MyRoomFurnitureTransform Transform { get; } = transform;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(RoomId);
        writer.Write(FurnitureId);
        Transform.Write(writer);
        return writer.ToBytes();
    }
}
