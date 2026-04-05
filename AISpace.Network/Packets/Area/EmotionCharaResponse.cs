using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class EmotionCharaResponse(uint objId, uint result) : IOutgoingPacket
{
    public uint ObjId { get; set; } = objId;
    public uint Result { get; set; } = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ObjId);
        writer.Write(Result);
        return writer.ToBytes();
    }
}
