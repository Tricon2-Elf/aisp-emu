using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Msg;

namespace AISpace.Common.Handlers.Msg;

public class CircleMemberJoinMemberHandler(ICircleRepository circles, SharedState state)
    : PacketHandlerBase<CircleMemberJoinMemberRequest, CircleMemberJoinMemberResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.CircleMemberJoinMemberRequest;
    public override PacketType ResponseType => PacketType.CircleMemberJoinMemberResponse;
    public override ServerType ServerType => ServerType.Msg;

    public override async Task<CircleMemberJoinMemberResponse?> HandleAsync(
        CircleMemberJoinMemberRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var result = await circles.InviteAsync(
            (int)session.CharacterId,
            (int)request.TargetAvatarId,
            checked((int)request.CircleId),
            ct
        );
        if (result.Result != CircleResult.Ok || result.Circle is null || result.JoinRequest is null)
            return new CircleMemberJoinMemberResponse((uint)result.Result);

        var notify = new CircleNotifyJoinRequest(
            session.CharacterId,
            circles.ToCircleData(result.Circle)
        ).ToBytes();
        foreach (
            var client in state.GetOnlineMsgClientsByCharacterIds(
                new[] { result.JoinRequest.TargetCharacterId }
            )
        )
            await client.SendAsync(PacketType.CircleNotifyJoinRequest, notify, ct);

        return new CircleMemberJoinMemberResponse(0);
    }
}
