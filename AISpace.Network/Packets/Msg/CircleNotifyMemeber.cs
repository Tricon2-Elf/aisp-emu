using AISpace.Network;
using AISpace.Network.Data;

namespace AISpace.Network.Packets.Msg;

public class CircleNotifyMember(uint circleId, List<CircleMemberData> members) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();

        writer.Write(circleId);
        writer.Write((uint)0);

        writer.Write((uint)members.Count);

        foreach (var member in members)
            writer.Write(member.ToBytes());

        writer.Write((uint)members.Count);

        foreach (var member in members)
            writer.Write((byte)1);

        return writer.ToBytes();
    }
}
