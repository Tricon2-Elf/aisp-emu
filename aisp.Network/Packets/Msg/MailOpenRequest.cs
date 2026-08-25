using aisp.Network;

namespace aisp.Network.Packets.Msg;

public class MailOpenRequest(ulong mailId, uint type) : IIncomingPacket<MailOpenRequest>
{
    public ulong MailId = mailId;
    public uint Type = type;

    public static MailOpenRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new MailOpenRequest(reader.ReadULong(), reader.ReadUInt());
    }
}
