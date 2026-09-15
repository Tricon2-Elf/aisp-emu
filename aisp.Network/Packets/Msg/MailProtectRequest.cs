using aisp.Network;

namespace aisp.Network.Packets.Msg;

public sealed class MailProtectRequest(ulong mailId) : IIncomingPacket<MailProtectRequest>
{
    public ulong MailId = mailId;

    public static MailProtectRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new MailProtectRequest(reader.ReadULong());
    }
}
