using aisp.Network;

namespace aisp.Network.Packets.Area;

public class AiDownloadListGetResponse(uint Result = 0, uint Downs = 0) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(Downs);
        return writer.ToBytes();
    }
}
