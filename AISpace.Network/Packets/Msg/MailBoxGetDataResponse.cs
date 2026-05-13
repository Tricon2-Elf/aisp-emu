using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class MailBoxGetDataResponse(uint result, uint mailCount) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(mailCount);
        return writer.ToBytes();
    }
}
