using aisp.Network;

namespace aisp.Network.Packets.Area;

public class ItemGetListResponse(uint Result) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
