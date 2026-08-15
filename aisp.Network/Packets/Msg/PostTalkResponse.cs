using aisp.Network;

namespace aisp.Network.Packets.Msg;

public class PostTalkResponse(uint messageId, uint result) : IOutgoingPacket
{
    public uint MessageId = messageId;
    public uint Result = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(MessageId);
        writer.Write(Result);
        return writer.ToBytes();
    }
}
