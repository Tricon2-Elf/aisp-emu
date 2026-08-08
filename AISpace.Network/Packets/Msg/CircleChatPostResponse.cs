using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class CircleChatPostResponse(uint messageId, uint result) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(messageId);
        writer.Write(result);
        return writer.ToBytes();
    }
}
