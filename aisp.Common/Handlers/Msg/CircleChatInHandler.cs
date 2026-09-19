using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

public class CircleChatInHandler(
    ICircleRepository circles,
    SharedState state,
    IChatLogRepository chatLog
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public const int HistoryLimit = 100;

    public PacketType RequestType => PacketType.CircleChatInRequest;
    public PacketType ResponseType => PacketType.CircleChatInResponse;
    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = CircleChatInRequest.FromBytes(payload.Span);
        if (request.CircleId > int.MaxValue || session.CharacterId == 0)
        {
            await session.SendAsync(
                ResponseType,
                new CircleChatInResponse((uint)CircleResult.NotMember).ToBytes(),
                ct
            );
            return;
        }

        var circleId = checked((int)request.CircleId);
        var membership = await circles.GetMembershipAsync(circleId, (int)session.CharacterId, ct);
        if (membership is null)
        {
            await session.SendAsync(
                ResponseType,
                new CircleChatInResponse((uint)CircleResult.NotMember).ToBytes(),
                ct
            );
            return;
        }

        state.EnterCircleChat(session.ConnectionId, circleId);
        uint[] onlineInChat =
        [
            .. state.GetCircleChatClients(circleId).Select(s => s.CharacterId).Distinct(),
        ];
        await session.SendAsync(
            ResponseType,
            new CircleChatInResponse(0, 1, onlineInChat).ToBytes(),
            ct
        );

        await CircleNotifyHelper.SendRosterAsync(circles, state, circleId, ct);

        var notify = new CircleNotifyChatIn(request.CircleId, session.CharacterId).ToBytes();
        foreach (var client in state.GetCircleChatClients(circleId))
        {
            if (client.ConnectionId != session.ConnectionId)
                _ = client.SendAsync(PacketType.CircleNotifyChatIn, notify, ct);
        }

        if (
            state.TryBeginCircleChatHistoryReplay(
                session.ConnectionId,
                circleId,
                membership.JoinedAt
            )
        )
        {
            var history = await chatLog.ListRecentCircleAsync(
                circleId,
                membership.JoinedAt,
                HistoryLimit,
                ct
            );
            foreach (var message in history)
            {
                await session.SendAsync(
                    PacketType.CircleChatForwardNotify,
                    new CircleChatForwardNotify(
                        checked((uint)message.CharacterId),
                        message.Message
                    ).ToBytes(),
                    ct
                );
            }
        }
    }
}
