using aisp.Network;

namespace aisp.Network.Packets.Area;

public class AdventureUploadRateGetResponse(uint RatePercent) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(RatePercent);
        return writer.ToBytes();
    }
}
