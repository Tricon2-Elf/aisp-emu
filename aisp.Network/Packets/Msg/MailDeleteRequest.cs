using aisp.Network;

namespace aisp.Network.Packets.Msg;

public sealed class MailDeleteRequest(ulong mailId, uint type) : IIncomingPacket<MailDeleteRequest>
{
    public ulong MailId = mailId;
    public uint Type = type;

    public static MailDeleteRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new MailDeleteRequest(reader.ReadULong(), reader.ReadUInt());
    }
}
