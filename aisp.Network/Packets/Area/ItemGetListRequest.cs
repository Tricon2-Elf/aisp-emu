using aisp.Network;

namespace aisp.Network.Packets.Area;

public class ItemGetListRequest : IIncomingPacket<ItemGetListRequest>
{
    public static ItemGetListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        return new ItemGetListRequest();
    }
}
