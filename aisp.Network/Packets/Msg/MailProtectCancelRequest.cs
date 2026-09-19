using aisp.Network;

namespace aisp.Network.Packets.Msg;

public sealed class MailProtectCancelRequest(ulong mailId)
    : IIncomingPacket<MailProtectCancelRequest>
{
    public ulong MailId = mailId;

    public static MailProtectCancelRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new MailProtectCancelRequest(reader.ReadULong());
    }
}
