using aisp.Network;

namespace aisp.Network.Packets.Msg;

public sealed class MailDeleteResponse(uint result, ulong mailId, uint type) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(mailId);
        writer.Write(type);
        return writer.ToBytes();
    }
}
