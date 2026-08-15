using aisp.Network;

namespace aisp.Network.Packets.Area;

public class EmotionCharaRequest(uint objId, uint emotionId) : IIncomingPacket<EmotionCharaRequest>
{
    public uint ObjId { get; set; } = objId;
    public uint EmotionId { get; set; } = emotionId;

    public static EmotionCharaRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new EmotionCharaRequest(reader.ReadUInt(), reader.ReadUInt());
    }
}
