using aisp.Network;

namespace aisp.Network.Packets.Msg;

public class AvatarDestroyResponse(uint result) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result); // 0
        return writer.ToBytes();
    }
}
