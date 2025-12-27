
namespace AISpace.Common.Network.Packets.Msg;

public class EnqueteAnswerResponse(uint result) : IPacket<EnqueteAnswerResponse>
{
    public static EnqueteAnswerResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        using var writer = new PacketWriter();
        writer.Write(result);
        return writer.ToBytes();
    }
}
