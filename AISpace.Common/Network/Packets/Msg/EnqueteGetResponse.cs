
using AISpace.Common.Game;

namespace AISpace.Common.Network.Packets.Msg;

public class EnqueteGetResponse(uint result, List<EnqueteData> Questions) : IPacket<EnqueteGetResponse>
{
    public static EnqueteGetResponse FromBytes(ReadOnlySpan<byte> data) => throw new NotImplementedException();

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write((uint)Questions.Count);
        foreach (var question in Questions)
            writer.Write(question.ToBytes());
        return writer.ToBytes();
    }
}
