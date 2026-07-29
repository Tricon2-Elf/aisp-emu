namespace AISpace.Network.Packets.Area;

/// <summary>recv_notify_myroom_use_furniture (0xF777).</summary>
public sealed class NotifyMyRoomUseFurniture(uint roomId, uint furnitureId) : IOutgoingPacket
{
    public uint RoomId { get; } = roomId;
    public uint FurnitureId { get; } = furnitureId;

    public byte[] ToBytes() => MyRoomFurniturePacketEncoding.WritePair(RoomId, FurnitureId);
}
