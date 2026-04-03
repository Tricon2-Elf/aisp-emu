using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class AiUploadRateGetResponse(uint Result = 1) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
