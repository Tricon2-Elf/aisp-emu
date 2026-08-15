using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Msg;

public class CircleNotifyJoinRequest(uint fromAvatarId, CircleData circle) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(fromAvatarId);
        circle.Write(writer);
        return writer.ToBytes();
    }
}
