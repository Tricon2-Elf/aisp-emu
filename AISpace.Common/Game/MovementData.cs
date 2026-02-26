namespace AISpace.Common.Game;

public enum MovementType : byte
{
    Stopped = 0,
    Walking = 1,
    Running = 3,
}

public class MovementData(float x, float y, float z, sbyte rotation, MovementType animation)
{
    public float X = x; public float Y = y; public float Z = z;
    public sbyte Rotation = rotation;
    public MovementType Animation = animation;

    public byte[] ToBytes()
    {
        var writer = new Network.PacketWriter();
        writer.Write(X); writer.Write(Y); writer.Write(Z);
        writer.Write(Rotation);
        writer.Write((byte)Animation);
        return writer.ToBytes();
    }

    public static MovementData FromBytes(ReadOnlySpan<byte> source)
    {
        var reader = new Network.PacketReader(source);
        return new MovementData(
            reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat(),
            reader.ReadSByte(), (MovementType)reader.ReadByte()
        );
    }
}
