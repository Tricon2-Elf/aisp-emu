using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

public class CircleMessageChangeHandler(
    ICircleRepository circles,
    SharedState state,
    IWordFilter wordFilter
)
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
        if (wordFilter.ContainsBlockedWord(WordFilterLevel.Complete, request.Message))
            return new CircleMessageChangeResponse((uint)CircleResult.Failed);

        var circleId = checked((int)request.CircleId);
        var result = await circles.UpdateMessageAsync(
            (int)session.CharacterId,
            circleId,
            request.Message,
            ct
        );
        if (result.Result != CircleResult.Ok || result.Circle is null)
            return new CircleMessageChangeResponse((uint)result.Result);

        // Notify: author name + date + message (mark icon is a separate markId dword / notify).
        var notify = new CircleNotifyMessageChange(
            request.CircleId,
            result.Circle.Mark,
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
