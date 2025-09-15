using System.Numerics;

namespace AISpace.Common.Game;

public class MoveData(Vector3 position, sbyte yaw, byte animation)
{
    public Vector3 Position = position;
    public sbyte yaw = yaw;
    public byte Animation = animation;
    //0 = standing still
    //1 = Walking
    //2 = Running
    public byte[] ToBytes()
    {
        using var writer = new Network.PacketWriter();
        writer.Write(Position.X, Position.Y, Position.Z);
        writer.Write(yaw);
        writer.Write(Animation);
        return writer.ToBytes();
    }

    public static MoveData FromBytes(ReadOnlySpan<byte> source)
    {
        var reader = new Network.PacketReader(source);
        var x = reader.ReadFloat();
        var y = reader.ReadFloat();
        var z = reader.ReadFloat();
        var yaw = reader.ReadSByte();
        var d = reader.ReadByte();
        var temp = new MoveData(new Vector3(x,y,z), yaw, d);
        return temp;

    }
}
