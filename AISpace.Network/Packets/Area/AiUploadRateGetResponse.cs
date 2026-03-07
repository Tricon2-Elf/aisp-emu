using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class AiUploadRateGetResponse(uint Result = 1) : IPacket<AiUploadRateGetResponse>
{
    public static AiUploadRateGetResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
