using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class PostTalkResponse(uint messageId, uint result) : IPacket<PostTalkResponse>
{
    public uint MessageId = messageId;
    public uint Result = result;

    public static PostTalkResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(MessageId);
        writer.Write(Result);
        return writer.ToBytes();
    }
}
