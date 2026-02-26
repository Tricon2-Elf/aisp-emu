using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Area;

/// <summary>Server notify: character is playing an emote. Client plays it when receiving this (recv_notify_emotion_chara).</summary>
public class NotifyEmotionChara(uint objId, uint emotionId) : IPacket<NotifyEmotionChara>
{
    public uint ObjId { get; set; } = objId;
    public uint EmotionId { get; set; } = emotionId;

    public static NotifyEmotionChara FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new NotifyEmotionChara(reader.ReadUInt(), reader.ReadUInt());
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ObjId);
        writer.Write(EmotionId);
        return writer.ToBytes();
    }
}
