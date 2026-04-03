using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class CircleGetDataRequest : IIncomingPacket<CircleGetDataRequest>
{
    // This request usually has no parameters, it just requests a list
    public static CircleGetDataRequest FromBytes(ReadOnlySpan<byte> data)
    {
        return new CircleGetDataRequest();
    }
}
