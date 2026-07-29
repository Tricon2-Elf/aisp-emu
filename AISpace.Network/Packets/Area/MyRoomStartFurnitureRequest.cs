using AISpace.Network;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// send_myroom_start_furniture (0x6A58), 4 bytes: UInt RoomId from the active MyRoom info struct.
/// </summary>
public class MyRoomStartFurnitureRequest(uint roomId) : IIncomingPacket<MyRoomStartFurnitureRequest>
{
    public const int WireSize = 4;

    public uint RoomId = roomId;

    public static MyRoomStartFurnitureRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != WireSize)
            throw new InvalidDataException(
                $"{nameof(MyRoomStartFurnitureRequest)} requires exactly {WireSize} bytes, received {data.Length}."
            );

        var reader = new PacketReader(data);
        return new MyRoomStartFurnitureRequest(reader.ReadUInt());
    }
}
