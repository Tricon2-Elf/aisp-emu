using AISpace.Network;
using AISpace.Network.Data;

namespace AISpace.Network.Packets.Msg;

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
