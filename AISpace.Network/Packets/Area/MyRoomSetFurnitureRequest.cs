using AISpace.Network.Data;

namespace AISpace.Network.Packets.Area;

/// <summary>send_myroom_set_furniture (0xAEFB): room ID, inventory serial ID, position, and two direction bytes.</summary>
public sealed class MyRoomSetFurnitureRequest(
    uint roomId,
    uint serialId,
    MyRoomFurnitureTransform transform
) : IIncomingPacket<MyRoomSetFurnitureRequest>
{
    public const int WireSize = 2 * sizeof(uint) + MyRoomFurnitureTransform.WireSize;

    public uint RoomId { get; } = roomId;
    public uint SerialId { get; } = serialId;
    public MyRoomFurnitureTransform Transform { get; } = transform;

    public static MyRoomSetFurnitureRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != WireSize)
            throw new InvalidDataException(
                $"{nameof(MyRoomSetFurnitureRequest)} requires exactly {WireSize} bytes, received {data.Length}."
            );

        var reader = new PacketReader(data);
        var roomId = reader.ReadUInt();
        var serialId = reader.ReadUInt();
        return new MyRoomSetFurnitureRequest(
            roomId,
            serialId,
            MyRoomFurnitureTransform.Read(ref reader)
        );
    }
}
