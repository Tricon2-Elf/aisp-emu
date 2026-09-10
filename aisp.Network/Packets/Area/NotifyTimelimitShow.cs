using aisp.Network;

namespace aisp.Network.Packets.Area;

public class NotifyTimelimitShow(uint tmEnd, uint remainSec) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(tmEnd);
        writer.Write(remainSec);
        return writer.ToBytes();
    }
}
