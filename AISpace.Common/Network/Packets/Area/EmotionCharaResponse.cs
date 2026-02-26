using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Area;

public class EmotionCharaResponse(uint objId, uint result) : IPacket<EmotionCharaResponse>
{
    public uint ObjId { get; set; } = objId;
    public uint Result { get; set; } = result;

    public static EmotionCharaResponse FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new EmotionCharaResponse(reader.ReadUInt(), reader.ReadUInt());
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ObjId);
        writer.Write(Result);
        return writer.ToBytes();
    }
}
