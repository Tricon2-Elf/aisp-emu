using aisp.Network;

namespace aisp.Network.Packets.Msg;

public class CircleGetDataRequest : IIncomingPacket<CircleGetDataRequest>
{
    public static CircleGetDataRequest FromBytes(ReadOnlySpan<byte> data) => new();
}
