using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

public class CircleMemberJoinMemberCancelHandler(ICircleRepository circles)
    : PacketHandlerBase<CircleMemberJoinMemberCancelRequest, CircleMemberJoinMemberCancelResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.CircleMemberJoinMemberCancelRequest;
    public override PacketType ResponseType => PacketType.CircleMemberJoinMemberCancelResponse;
    public override ServerType ServerType => ServerType.Msg;

    public override async Task<CircleMemberJoinMemberCancelResponse?> HandleAsync(
        CircleMemberJoinMemberCancelRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var result = await circles.CancelInviteAsync((int)session.CharacterId, ct);
        return new CircleMemberJoinMemberCancelResponse((uint)result.Result);
    }
}
