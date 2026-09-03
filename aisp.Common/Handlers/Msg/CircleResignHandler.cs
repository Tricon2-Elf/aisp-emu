using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Services;
using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

public class CircleResignHandler(ICircleRepository circles, SharedState state)
    : PacketHandlerBase<CircleResignRequest, CircleResignResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.CircleResignRequest;
    public override PacketType ResponseType => PacketType.CircleResignResponse;
    public override ServerType ServerType => ServerType.Msg;

    public override async Task<CircleResignResponse?> HandleAsync(
        CircleResignRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var circleId = checked((int)request.CircleId);
        var circle = await circles.GetByIdAsync(circleId, ct);
        if (
            circle is not null
            && ModerationService.IsModeratorsCircle(circle.Name)
            && session.User?.Role.HasPortalAccess() == true
        )
            return new CircleResignResponse((uint)CircleResult.NotAuthorized);

        var result = await circles.ResignAsync((int)session.CharacterId, circleId, ct);
        if (result.Result != CircleResult.Ok)
            return new CircleResignResponse((uint)result.Result);

        if (!result.CircleDeleted)
        {
            var resignPayload = new CircleNotifyResignMember(
                request.CircleId,
                session.CharacterId
            ).ToBytes();
            await CircleNotifyHelper.NotifyMembersAsync(
                circles,
                state,
                circleId,
                PacketType.CircleNotifyResignMember,
                resignPayload,
                ct
            );

            if (
                result.PreviousLeaderCharacterId is not null
                && result.NewLeaderCharacterId is not null
            )
            {
                var leaderPayload = new CircleNotifyLeaderChange(
                    request.CircleId,
                    (uint)result.PreviousLeaderCharacterId.Value,
                    (uint)result.NewLeaderCharacterId.Value,
                    reason: 1
                ).ToBytes();
                await CircleNotifyHelper.NotifyMembersAsync(
                    circles,
                    state,
                    circleId,
                    PacketType.CircleNotifyLeaderChange,
                    leaderPayload,
                    ct
                );
                foreach (
                    var client in state.GetOnlineMsgClientsByCharacterId(
                        result.NewLeaderCharacterId.Value
                    )
                )
                {
                    await client.SendAsync(
                        PacketType.CircleLeaderChangeResponse,
                        new CircleLeaderChangeResponse(0).ToBytes(),
                        ct
                    );
                }
            }

            await CircleNotifyHelper.SendRosterAsync(circles, state, circleId, ct);
        }

        if (state.TryGetCircleChat(session.ConnectionId, out var chatId) && chatId == circleId)
            state.LeaveCircleChat(session.ConnectionId);

        return new CircleResignResponse(0);
    }
}
