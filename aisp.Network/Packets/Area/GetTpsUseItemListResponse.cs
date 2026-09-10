using aisp.Network;

namespace aisp.Network.Packets.Area;

public class GetTpsUseItemListResponse(uint result = 0) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(0u);
        return writer.ToBytes();
    }
}
