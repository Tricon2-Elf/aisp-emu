using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

public class CircleChangeCoreAuthorityHandler(ICircleRepository circles, SharedState state)
    : PacketHandlerBase<CircleChangeCoreAuthorityRequest, CircleChangeCoreAuthorityResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.CircleChangeCoreAuthorityRequest;
    public override PacketType ResponseType => PacketType.CircleChangeCoreAuthorityResponse;
    public override ServerType ServerType => ServerType.Msg;

    public override async Task<CircleChangeCoreAuthorityResponse?> HandleAsync(
        CircleChangeCoreAuthorityRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var circleId = checked((int)request.CircleId);
        var result = await circles.SetCoreAuthorityAsync(
            (int)session.CharacterId,
            circleId,
            (int)request.AvatarId,
            request.Auth,
            ct
        );
        if (result.Result != CircleResult.Ok)
            return new CircleChangeCoreAuthorityResponse((uint)result.Result);

        var notify = new CircleNotifyChangeCoreAuthority(
            request.CircleId,
            request.AvatarId,
            request.Auth
        ).ToBytes();
        await CircleNotifyHelper.NotifyMembersAsync(
            circles,
            state,
            circleId,
            PacketType.CircleNotifyChangeCoreAuthority,
            notify,
            ct
        );
        await CircleNotifyHelper.SendRosterAsync(circles, state, circleId, ct);
        return new CircleChangeCoreAuthorityResponse(0);
    }
}
