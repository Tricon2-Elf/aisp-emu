using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

public class CircleMemberKickHandler(ICircleRepository circles, SharedState state)
    : PacketHandlerBase<CircleMemberKickRequest, CircleMemberKickResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.CircleMemberKickRequest;
    public override PacketType ResponseType => PacketType.CircleMemberKickResponse;
    public override ServerType ServerType => ServerType.Msg;

    public override async Task<CircleMemberKickResponse?> HandleAsync(
        CircleMemberKickRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var circleId = checked((int)request.CircleId);
        var result = await circles.KickAsync(
            (int)session.CharacterId,
            circleId,
            (int)request.AvatarId,
            ct
        );
        if (result.Result != CircleResult.Ok)
            return new CircleMemberKickResponse((uint)result.Result);

        var kickPayload = new CircleNotifyKick(request.CircleId).ToBytes();
        // Notify the kicked character specifically, then remaining members.
        foreach (
            var client in state.GetOnlineMsgClientsByCharacterId((int)request.AvatarId)
        )
        {
            await client.SendAsync(PacketType.CircleNotifyKick, kickPayload, ct);
            if (state.TryGetCircleChat(client.ConnectionId, out var chatId) && chatId == circleId)
                state.LeaveCircleChat(client.ConnectionId);
        }

        await CircleNotifyHelper.NotifyMembersAsync(
            circles,
            state,
            circleId,
            PacketType.CircleNotifyKick,
            kickPayload,
            ct
        );
        await CircleNotifyHelper.SendRosterAsync(circles, state, circleId, ct);
        return new CircleMemberKickResponse(0);
    }
}
