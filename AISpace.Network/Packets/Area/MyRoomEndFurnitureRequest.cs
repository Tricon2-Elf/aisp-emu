using AISpace.Network;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// send_myroom_end_furniture (0xB739), 4 bytes: UInt RoomId (OwnerId from the active MyRoom info struct).
/// </summary>
public class MyRoomEndFurnitureRequest(uint roomId) : IIncomingPacket<MyRoomEndFurnitureRequest>
{
    public uint RoomId = roomId;

    public static MyRoomEndFurnitureRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new MyRoomEndFurnitureRequest(reader.ReadUInt());
    }
}
