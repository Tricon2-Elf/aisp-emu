using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Msg;

namespace AISpace.Common.Handlers.Msg;

public class CircleChatInHandler(ICircleRepository circles, SharedState state)
    : PacketHandlerBase<CircleChatInRequest, CircleChatInResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.CircleChatInRequest;
    public override PacketType ResponseType => PacketType.CircleChatInResponse;
    public override ServerType ServerType => ServerType.Msg;

    public override async Task<CircleChatInResponse?> HandleAsync(
        CircleChatInRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var circleId = checked((int)request.CircleId);
        var membership = await circles.GetMembershipAsync(circleId, (int)session.CharacterId, ct);
        if (membership is null)
            return new CircleChatInResponse((uint)CircleResult.NotMember);

        state.EnterCircleChat(session.ConnectionId, circleId);
        var members = await circles.GetMembersAsync(circleId, ct);
        uint[] onlineInChat =
        [
            .. state.GetCircleChatClients(circleId).Select(s => s.CharacterId).Distinct(),
        ];

        var notify = new CircleNotifyChatIn(request.CircleId, session.CharacterId).ToBytes();
        foreach (var client in state.GetCircleChatClients(circleId))
        {
            if (client.ConnectionId != session.ConnectionId)
                await client.SendAsync(PacketType.CircleNotifyChatIn, notify, ct);
        }

        return new CircleChatInResponse(0, 1, onlineInChat);
    }
}
