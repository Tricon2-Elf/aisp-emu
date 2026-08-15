using aisp.Network;

namespace aisp.Network.Packets.Msg;

public class CircleNotifyLeaderChange(
    ulong circleId,
    uint fromAvatarId,
    uint distAvatarId,
    uint reason
) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(circleId);
        writer.Write(fromAvatarId);
        writer.Write(distAvatarId);
        writer.Write(reason);
        return writer.ToBytes();
    }
}
