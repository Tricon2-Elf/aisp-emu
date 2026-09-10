using aisp.Network;

namespace aisp.Network.Packets.Area;

public class GetTpsUseItemListRequest : IIncomingPacket<GetTpsUseItemListRequest>
{
    public static GetTpsUseItemListRequest FromBytes(ReadOnlySpan<byte> data) => new();
}
