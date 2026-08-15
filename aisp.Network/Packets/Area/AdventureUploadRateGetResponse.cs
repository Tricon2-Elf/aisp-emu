using aisp.Network;

namespace aisp.Network.Packets.Area;

public class AdventureUploadRateGetResponse(uint Result = 1) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
