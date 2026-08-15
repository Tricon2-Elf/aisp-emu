using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// send_myroom_end_furniture (0xB739), 4 bytes: UInt RoomId from the active MyRoom info struct.
/// </summary>
public class MyRoomEndFurnitureRequest(uint roomId) : IIncomingPacket<MyRoomEndFurnitureRequest>
{
    public const int WireSize = 4;

    public uint RoomId = roomId;

    public static MyRoomEndFurnitureRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != WireSize)
            throw new InvalidDataException(
                $"{nameof(MyRoomEndFurnitureRequest)} requires exactly {WireSize} bytes, received {data.Length}."
            );

        var reader = new PacketReader(data);
        return new MyRoomEndFurnitureRequest(reader.ReadUInt());
    }
}
