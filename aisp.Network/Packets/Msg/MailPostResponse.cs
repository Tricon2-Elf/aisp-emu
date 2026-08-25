using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Msg;

public class MailPostResponse(uint result, MailData mail) : IOutgoingPacket
{
    public uint Result = result;
    public MailData Mail = mail;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        Mail.Write(writer);
        return writer.ToBytes();
    }
}
