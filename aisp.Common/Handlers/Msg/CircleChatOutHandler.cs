using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

public class CircleChatOutHandler(SharedState state)
    : PacketHandlerBase<CircleChatOutRequest, CircleChatOutResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.CircleChatOutRequest;
    public override PacketType ResponseType => PacketType.CircleChatOutResponse;
    public override ServerType ServerType => ServerType.Msg;

    public override Task<CircleChatOutResponse?> HandleAsync(
        CircleChatOutRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (state.TryGetCircleChat(session.ConnectionId, out var circleId))
        {
            state.LeaveCircleChat(session.ConnectionId);
            var notify = new CircleNotifyChatOut((ulong)circleId, session.CharacterId).ToBytes();
            foreach (var client in state.GetCircleChatClients(circleId))
                _ = client.SendAsync(PacketType.CircleNotifyChatOut, notify, ct);
        }

        return Task.FromResult<CircleChatOutResponse?>(new CircleChatOutResponse(0));
    }
}
