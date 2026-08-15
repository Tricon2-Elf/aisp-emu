using aisp.Network;

namespace aisp.Network.Packets.Msg;

public class CircleNotifyResignMember(ulong circleId, uint avatarId) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(circleId);
        writer.Write(avatarId);
        return writer.ToBytes();
    }
}
