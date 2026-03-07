using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class EmotionGetObtainedListResponse(uint Result, List<uint> Ids) : IPacket<EmotionGetObtainedListResponse>
{
    public static EmotionGetObtainedListResponse FromBytes(ReadOnlySpan<byte> data) => throw new NotImplementedException();

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
