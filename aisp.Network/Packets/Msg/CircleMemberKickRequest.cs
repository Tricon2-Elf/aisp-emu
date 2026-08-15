using aisp.Network;

namespace aisp.Network.Packets.Msg;

public class CircleMemberKickRequest : IIncomingPacket<CircleMemberKickRequest>
{
    public ulong CircleId;
    public uint AvatarId;

    public static CircleMemberKickRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new CircleMemberKickRequest
        {
            CircleId = reader.ReadULong(),
            AvatarId = reader.ReadUInt(),
        };
    }
}
