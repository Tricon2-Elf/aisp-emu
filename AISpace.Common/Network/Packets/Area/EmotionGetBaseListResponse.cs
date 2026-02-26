using AISpace.Common.Game;

namespace AISpace.Common.Network.Packets.Area;

public class EmotionGetBaseListResponse(uint Result, List<EmotionData> Emotions) : IPacket<EmotionGetBaseListResponse>
{
    public static EmotionGetBaseListResponse FromBytes(ReadOnlySpan<byte> data) => throw new NotImplementedException();

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
