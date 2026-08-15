using aisp.Network;

namespace aisp.Network.Packets.Area;

public class EmotionGetObtainedListResponse(uint Result, List<uint> Ids) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write((uint)Ids.Count);
        foreach (var id in Ids)
        {
            writer.Write(id);
        }
        return writer.ToBytes();
    }
}
