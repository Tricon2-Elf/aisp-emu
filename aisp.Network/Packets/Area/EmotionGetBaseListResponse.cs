using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public class EmotionGetBaseListResponse(uint Result, List<EmotionData> Emotions) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write((uint)Emotions.Count);
        foreach (var emo in Emotions)
        {
            emo.Write(writer);
        }
        return writer.ToBytes();
    }
}
