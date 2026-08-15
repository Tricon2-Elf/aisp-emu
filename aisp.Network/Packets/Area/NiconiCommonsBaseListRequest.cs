using aisp.Network;

namespace aisp.Network.Packets.Area;

public class NiconiCommonsBaseListRequest : IIncomingPacket<NiconiCommonsBaseListRequest>
{
    public static NiconiCommonsBaseListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }
}
