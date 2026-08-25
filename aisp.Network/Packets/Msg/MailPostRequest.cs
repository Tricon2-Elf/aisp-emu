using aisp.Network;

namespace aisp.Network.Packets.Msg;

public class MailPostRequest(uint distId, string distName, string subject, string body)
    : IIncomingPacket<MailPostRequest>
{
    public uint DistId = distId;
    public string DistName = distName;
    public string Subject = subject;
    public string Body = body;

    public static MailPostRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new MailPostRequest(
            reader.ReadUInt(),
            reader.ReadString(),
            reader.ReadString(),
            reader.ReadString()
        );
    }
}
