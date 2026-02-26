using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Area;

public class EmotionCharaRequest(uint objId, uint emotionId) : IPacket<EmotionCharaRequest>
{
    public uint ObjId { get; set; } = objId;
    public uint EmotionId { get; set; } = emotionId;

    public static EmotionCharaRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new EmotionCharaRequest(reader.ReadUInt(), reader.ReadUInt());
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ObjId);
        writer.Write(EmotionId);
        return writer.ToBytes();
    }
}
