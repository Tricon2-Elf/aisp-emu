namespace AISpace.Network.Data;

/// <summary>
/// The 34-byte Furniture structure consumed by recv_notify_myroom_furniture and
/// recv_notify_myroom_set_furniture. Direction values are already wire bytes.
/// </summary>
public readonly record struct MyRoomFurnitureData(uint RoomId, uint FurnitureId, uint PlacementState, uint SerialId, float X, float Y, float Z, byte DirectionX, byte DirectionY, uint Active)
{
    public const int WireSize = 34;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(RoomId);
        writer.Write(FurnitureId);
        writer.Write(PlacementState);
        writer.Write(SerialId);
        writer.Write(X);
        writer.Write(Y);
        writer.Write(Z);
        writer.Write(DirectionX);
        writer.Write(DirectionY);
        writer.Write(Active);
        return writer.ToBytes();
    }
}
