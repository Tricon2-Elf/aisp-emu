using aisp.Network;

namespace aisp.Network.Packets.Msg;

public class CircleNotifyChangeCoreAuthority(ulong circleId, uint avatarId, uint auth)
    : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(circleId);
        writer.Write(avatarId);
        writer.Write(auth);
        return writer.ToBytes();
    }
}
