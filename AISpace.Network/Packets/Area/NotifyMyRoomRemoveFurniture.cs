namespace AISpace.Network.Packets.Area;

/// <summary>recv_notify_myroom_remove_furniture (0x7A75).</summary>
public sealed class NotifyMyRoomRemoveFurniture(uint roomId, uint furnitureId) : IOutgoingPacket
{
    public uint RoomId { get; } = roomId;
    public uint FurnitureId { get; } = furnitureId;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(RoomId);
        writer.Write(FurnitureId);
        return writer.ToBytes();
    }
}
