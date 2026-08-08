using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Msg;

namespace AISpace.Common.Handlers.Msg;

public class CircleMarkChangeHandler(ICircleRepository circles, SharedState state)
    : PacketHandlerBase<CircleMarkChangeRequest, CircleMarkChangeResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.CircleMarkChangeRequest;
    public override PacketType ResponseType => PacketType.CircleMarkChangeResponse;
    public override ServerType ServerType => ServerType.Msg;

    public override async Task<CircleMarkChangeResponse?> HandleAsync(
        CircleMarkChangeRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var circleId = checked((int)request.CircleId);
        var result = await circles.UpdateMarkAsync(
            (int)session.CharacterId,
            circleId,
            request.MarkId,
            ct
        );
        if (result.Result != CircleResult.Ok)
            return new CircleMarkChangeResponse((uint)result.Result);

        var notify = new CircleNotifyMarkChange(request.CircleId, request.MarkId).ToBytes();
        await CircleNotifyHelper.NotifyMembersAsync(
            circles,
            state,
            circleId,
            PacketType.CircleNotifyMarkChange,
            notify,
            ct
        );
        return new CircleMarkChangeResponse(0);
    }
}
