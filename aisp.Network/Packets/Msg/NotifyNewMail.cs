using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Msg;

public class NotifyNewMail(MailData mail) : IOutgoingPacket
{
    public MailData Mail = mail;

    public byte[] ToBytes() => Mail.ToBytes();
}
