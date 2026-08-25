using aisp.Network;

namespace aisp.Network.Packets.Msg;

public class MailOpenResponse(uint result, ulong mailId, uint type) : IOutgoingPacket
{
    public uint Result = result;
    public ulong MailId = mailId;
    public uint Type = type;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(MailId);
        writer.Write(Type);
        return writer.ToBytes();
    }
}
