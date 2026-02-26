namespace AISpace.Common.Network.Packets.Msg;

public class CircleGetDataRequest : IPacket<CircleGetDataRequest>
{
    // This request usually has no parameters, it just requests a list
    public static CircleGetDataRequest FromBytes(ReadOnlySpan<byte> data)
    {
        return new CircleGetDataRequest();
    }

    public byte[] ToBytes() => [];
}
