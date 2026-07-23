using AISpace.Network;

namespace AISpace.Network.Data;

public enum MovementType : byte
{
    Stopped = 0,
    Walking = 1,
    Running = 3,
}

/// <summary>Movement sample. <see cref="Rotation"/> is degrees; converted to wire half-degrees in <see cref="ToBytes"/>.</summary>
public class MovementData(float x, float y, float z, int rotation, MovementType animation)
{
    public float X = x;
    public float Y = y;
    public float Z = z;

    /// <summary>Facing in degrees (0–359). Not the raw wire byte.</summary>
    public int Rotation = rotation;

    public MovementType Animation = animation;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(X);
        writer.Write(Y);
        writer.Write(Z);
        writer.Write(YawEncoding.ToWireSByte(Rotation));
        writer.Write((byte)Animation);
        return writer.ToBytes();
    }

    public static MovementData FromBytes(ReadOnlySpan<byte> source)
    {
        var reader = new PacketReader(source);
        return new MovementData(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat(), YawEncoding.FromWireSByte(reader.ReadSByte()), (MovementType)reader.ReadByte());
    }
}
