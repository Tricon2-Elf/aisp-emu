using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class CircleNotifyMemberLogin(ulong circleId, uint avatarId) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(circleId);
        writer.Write(avatarId);
        return writer.ToBytes();
    }
}
