using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class EquipOrderListRequest : IPacket<EquipOrderListRequest>
{
    public static EquipOrderListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        throw new NotImplementedException();
    }
}
