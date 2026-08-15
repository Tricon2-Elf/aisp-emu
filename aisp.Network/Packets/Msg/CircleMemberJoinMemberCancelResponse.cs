using aisp.Network;

namespace aisp.Network.Packets.Msg;

public class CircleMemberJoinMemberCancelResponse(uint result) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        return writer.ToBytes();
    }
}
