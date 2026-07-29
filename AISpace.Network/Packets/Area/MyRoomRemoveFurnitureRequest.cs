namespace AISpace.Network.Packets.Area;

/// <summary>send_myroom_remove_furniture (0xD0DB): room ID and placed-furniture ID.</summary>
public sealed class MyRoomRemoveFurnitureRequest(uint roomId, uint furnitureId) : IIncomingPacket<MyRoomRemoveFurnitureRequest>
{
    public const int WireSize = 8;

    public uint RoomId { get; } = roomId;
    public uint FurnitureId { get; } = furnitureId;

    public static MyRoomRemoveFurnitureRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != WireSize)
            throw new InvalidDataException($"{nameof(MyRoomRemoveFurnitureRequest)} requires exactly {WireSize} bytes, received {data.Length}.");

        var reader = new PacketReader(data);
        return new MyRoomRemoveFurnitureRequest(reader.ReadUInt(), reader.ReadUInt());
    }
}
