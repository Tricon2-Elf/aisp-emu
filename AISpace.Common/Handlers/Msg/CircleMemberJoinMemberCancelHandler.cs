using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Msg;

namespace AISpace.Common.Handlers.Msg;

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
