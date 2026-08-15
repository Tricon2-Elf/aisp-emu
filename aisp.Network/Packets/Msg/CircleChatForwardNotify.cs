using aisp.Network;

namespace aisp.Network.Packets.Msg;

public class CircleChatForwardNotify(uint fromAvatarId, string message) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(fromAvatarId);
        writer.Write(message, "utf-8");
        return writer.ToBytes();
    }
}
