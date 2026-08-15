using aisp.Network;

namespace aisp.Network.Packets.Msg;

public class CircleNotifyAddMember(ulong circleId, uint avatarId, string name) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(circleId);
        writer.Write(avatarId);
        writer.Write(name, "utf-8");
        return writer.ToBytes();
    }
}
