using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class CircleMemberJoinMemberCancelRequest
    : IIncomingPacket<CircleMemberJoinMemberCancelRequest>
{
    public static CircleMemberJoinMemberCancelRequest FromBytes(ReadOnlySpan<byte> data) => new();
}
