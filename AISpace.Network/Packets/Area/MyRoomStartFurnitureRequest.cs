using AISpace.Network;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// send_myroom_start_furniture (0x6A58), 4 bytes: UInt RoomId (OwnerId from the active MyRoom info struct).
/// </summary>
public class MyRoomStartFurnitureRequest(uint roomId) : IIncomingPacket<MyRoomStartFurnitureRequest>
{
    public uint RoomId = roomId;

    public static MyRoomStartFurnitureRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new MyRoomStartFurnitureRequest(reader.ReadUInt());
    }
}
