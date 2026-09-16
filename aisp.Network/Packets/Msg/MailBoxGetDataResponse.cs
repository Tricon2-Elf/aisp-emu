using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Msg;

public class MailBoxGetDataResponse(uint result, IReadOnlyList<MailData> mail) : IOutgoingPacket
{
    public uint Result = result;
    public IReadOnlyList<MailData> Mail = mail;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(checked((uint)Mail.Count));
        foreach (var message in Mail)
            message.Write(writer);
        return writer.ToBytes();
    }
}
