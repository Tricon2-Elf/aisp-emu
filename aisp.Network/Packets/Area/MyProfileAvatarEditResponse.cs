using aisp.Network;

namespace aisp.Network.Packets.Area;

public class MyProfileAvatarEditResponse(uint result) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        return writer.ToBytes();
    }
}
