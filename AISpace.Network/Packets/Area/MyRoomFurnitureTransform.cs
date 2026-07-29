namespace AISpace.Network.Packets.Area;

/// <summary>
/// Position and direction portion shared by the set/update furniture requests.
/// Direction values are the client's half-degree byte representation.
/// </summary>
public readonly record struct MyRoomFurnitureTransform(float X, float Y, float Z, byte DirectionX, byte DirectionY)
{
    public const int WireSize = 14;

    internal static MyRoomFurnitureTransform Read(ref PacketReader reader) => new(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat(), reader.ReadByte(), reader.ReadByte());

    internal void Write(PacketWriter writer)
    {
        writer.Write(X);
        writer.Write(Y);
        writer.Write(Z);
        writer.Write(DirectionX);
        writer.Write(DirectionY);
    }
}
