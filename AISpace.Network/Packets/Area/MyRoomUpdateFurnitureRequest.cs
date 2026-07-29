using AISpace.Network.Data;

namespace AISpace.Network.Packets.Area;

/// <summary>send_myroom_update_furniture (0x6405): room ID, placed-furniture ID, position, and two direction bytes.</summary>
public sealed class MyRoomUpdateFurnitureRequest(
    uint roomId,
    uint furnitureId,
    MyRoomFurnitureTransform transform
) : IIncomingPacket<MyRoomUpdateFurnitureRequest>
{
    public const int WireSize = 2 * sizeof(uint) + MyRoomFurnitureTransform.WireSize;

    public uint RoomId { get; } = roomId;
    public uint FurnitureId { get; } = furnitureId;
    public MyRoomFurnitureTransform Transform { get; } = transform;

    public static MyRoomUpdateFurnitureRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != WireSize)
            throw new InvalidDataException(
                $"{nameof(MyRoomUpdateFurnitureRequest)} requires exactly {WireSize} bytes, received {data.Length}."
            );

        var reader = new PacketReader(data);
        var roomId = reader.ReadUInt();
        var furnitureId = reader.ReadUInt();
        return new MyRoomUpdateFurnitureRequest(
            roomId,
            furnitureId,
            MyRoomFurnitureTransform.Read(ref reader)
        );
    }
}
