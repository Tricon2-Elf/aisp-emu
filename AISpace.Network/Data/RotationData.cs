namespace AISpace.Network.Data;

public class RotationData(float X, float Y, float Z, float W)
{

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(X);
        writer.Write(Y);
        writer.Write(Z);
        writer.Write(W);
        return writer.ToBytes();
    }
}
