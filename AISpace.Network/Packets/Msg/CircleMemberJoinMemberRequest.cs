using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class CircleMemberJoinMemberRequest : IIncomingPacket<CircleMemberJoinMemberRequest>
{
    public uint TargetAvatarId;
    public ulong CircleId;

    public static CircleMemberJoinMemberRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new CircleMemberJoinMemberRequest
        {
            TargetAvatarId = reader.ReadUInt(),
            CircleId = reader.ReadULong(),
        };
    }
}
