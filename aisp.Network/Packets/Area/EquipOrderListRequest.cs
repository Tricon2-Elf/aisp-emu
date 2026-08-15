using aisp.Network;

namespace aisp.Network.Packets.Area;

public class EquipOrderListRequest : IIncomingPacket<EquipOrderListRequest>
{
    public static EquipOrderListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }
}
