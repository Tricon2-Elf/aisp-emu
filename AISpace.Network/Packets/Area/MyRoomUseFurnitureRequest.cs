using AISpace.Network;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// send_myroom_use_furniture (0x2231). Sent when the player clicks furniture whose furniture.csv
/// アクション is 4 (sub_4843A0 → sub_7B2210). 12-byte payload:
///   UInt RoomId   - myroom owner id
///   UInt FurnId   - furniture serial id
///   UInt Reason   - 1 when furniture ActiveFlag byte (+164) is 0, else 0
/// </summary>
public class MyRoomUseFurnitureRequest(uint roomId, uint furnId, uint reason)
    : IIncomingPacket<MyRoomUseFurnitureRequest>
{
    public const int WireSize = 12;

    public uint RoomId = roomId;
    public uint FurnId = furnId;
    public uint Reason = reason;

    public static MyRoomUseFurnitureRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != WireSize)
            throw new InvalidDataException(
                $"{nameof(MyRoomUseFurnitureRequest)} requires exactly {WireSize} bytes, received {data.Length}."
            );

        var reader = new PacketReader(data);
        return new MyRoomUseFurnitureRequest(
            reader.ReadUInt(),
            reader.ReadUInt(),
            reader.ReadUInt()
        );
    }
}
