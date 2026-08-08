using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class CircleGetDataRequest : IIncomingPacket<CircleGetDataRequest>
{
    public static CircleGetDataRequest FromBytes(ReadOnlySpan<byte> data) => new();
}
