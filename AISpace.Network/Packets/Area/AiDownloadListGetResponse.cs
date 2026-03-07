using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class AiDownloadListGetResponse(uint Result = 0, uint Downs = 0) : IPacket<AiDownloadListGetResponse>
{
    public static AiDownloadListGetResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(Downs);
        return writer.ToBytes();
    }
}
