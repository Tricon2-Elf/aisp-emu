using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

public class CircleCreateHandler(ICircleRepository circles, SharedState state)
    : PacketHandlerBase<CircleCreateRequest, CircleCreateResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.CircleCreateRequest;
    public override PacketType ResponseType => PacketType.CircleCreateResponse;
    public override ServerType ServerType => ServerType.Msg;

    public override async Task<CircleCreateResponse?> HandleAsync(
        CircleCreateRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (session.CharacterId == 0)
            return new CircleCreateResponse((uint)CircleResult.Failed, null);

        var result = await circles.CreateAsync(
            (int)session.CharacterId,
            request.Name,
            request.MarkId,
            ct
        );
        if (result.Result != CircleResult.Ok || result.Circle is null)
            return new CircleCreateResponse((uint)result.Result, null);

        await CircleNotifyHelper.SendRosterAsync(circles, state, result.Circle.Id, ct);
        return new CircleCreateResponse(0, circles.ToCircleData(result.Circle));
    }
}
