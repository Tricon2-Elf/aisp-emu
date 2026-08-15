using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>Server notify: character is playing an emote. Client plays it when receiving this (recv_notify_emotion_chara).</summary>
public class NotifyEmotionChara(uint objId, uint emotionId) : IOutgoingPacket
{
    public uint ObjId { get; set; } = objId;
    public uint EmotionId { get; set; } = emotionId;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ObjId);
        writer.Write(EmotionId);
        return writer.ToBytes();
    }
}
