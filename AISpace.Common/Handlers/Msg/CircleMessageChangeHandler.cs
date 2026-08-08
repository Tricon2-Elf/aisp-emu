using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Msg;

namespace AISpace.Common.Handlers.Msg;

public class CircleMessageChangeHandler(ICircleRepository circles, SharedState state)
    : PacketHandlerBase<CircleMessageChangeRequest, CircleMessageChangeResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.CircleMessageChangeRequest;
    public override PacketType ResponseType => PacketType.CircleMessageChangeResponse;
    public override ServerType ServerType => ServerType.Msg;

    public override async Task<CircleMessageChangeResponse?> HandleAsync(
        CircleMessageChangeRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var circleId = checked((int)request.CircleId);
        var result = await circles.UpdateMessageAsync(
            (int)session.CharacterId,
            circleId,
            request.Message,
            ct
        );
        if (result.Result != CircleResult.Ok || result.Circle is null)
            return new CircleMessageChangeResponse((uint)result.Result);

        var name = session.Character?.Name ?? string.Empty;
        var notify = new CircleNotifyMessageChange(
            request.CircleId,
            name,
            result.Circle.MessageDate,
            result.Circle.Message
        ).ToBytes();
        await CircleNotifyHelper.NotifyMembersAsync(
            circles,
            state,
            circleId,
            PacketType.CircleNotifyMessageChange,
            notify,
            ct
        );
        return new CircleMessageChangeResponse(0);
    }
}
