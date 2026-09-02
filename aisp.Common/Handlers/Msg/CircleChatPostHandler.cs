using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Localisation;
using aisp.Network;
using aisp.Network.Packets.Msg;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Msg;

public class CircleChatPostHandler(
    ILogger<CircleChatPostHandler> logger,
    ICircleRepository circles,
    SharedState state,
    IWordFilter wordFilter,
    ITextLocaliser localiser,
    IChatLogRepository chatLog
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.CircleChatPostRequest;
    public PacketType ResponseType => PacketType.CircleChatPostResponse;
    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var req = CircleChatPostRequest.FromBytes(payload.Span);
        if (!state.TryGetCircleChat(session.ConnectionId, out var circleId))
        {
            await session.SendAsync(
                ResponseType,
                new CircleChatPostResponse(req.MessageId, (uint)CircleResult.Failed).ToBytes(),
                ct
            );
            return;
        }

        var membership = await circles.GetMembershipAsync(circleId, (int)session.CharacterId, ct);
        if (membership is null)
        {
            await session.SendAsync(
                ResponseType,
                new CircleChatPostResponse(req.MessageId, (uint)CircleResult.NotMember).ToBytes(),
                ct
            );
            return;
        }

        if (wordFilter.ContainsBlockedWord(WordFilterLevel.NoSlurs, req.Message))
        {
            logger.LogWarning(
                "Rejecting circle chat from character {CharacterId} in circle {CircleId}: blocked message",
                session.CharacterId,
                circleId
            );
            await session.SendAsync(
                ResponseType,
                new CircleChatPostResponse(req.MessageId, (uint)CircleResult.Failed).ToBytes(),
                ct
            );
            await chatLog.AddAsync(
                ChatLogCapture.FromSession(
                    session,
                    state,
                    ChatLogKind.Circle,
                    req.Message,
                    circleId: circleId,
                    rejected: true
                ),
                ct
            );
            await SystemNotice.SendAsync(session, localiser.Get(session, L.Chat.SlurRejected), ct);
            return;
        }

        var fromId = session.CharacterId;
        if (fromId == 0)
        {
            var areaSession = state.GetAreaSessionByUserId(session.UserId);
            fromId = areaSession?.CharacterId ?? 0;
        }

        logger.LogDebug(
            "Circle chat from character {CharacterId} in circle {CircleId}",
            fromId,
            circleId
        );

        await chatLog.AddAsync(
            ChatLogCapture.FromSession(
                session,
                state,
                ChatLogKind.Circle,
                req.Message,
                circleId: circleId
            ),
            ct
        );

        await session.SendAsync(
            ResponseType,
            new CircleChatPostResponse(req.MessageId, 0).ToBytes(),
            ct
        );

        await CircleNotifyHelper.SendRosterAsync(circles, state, circleId, ct);

        var forward = new CircleChatForwardNotify(fromId, req.Message).ToBytes();
        var members = await circles.GetMembersAsync(circleId, ct);
        foreach (
            var client in state.GetOnlineMsgClientsByCharacterIds(
                members.Select(m => m.CharacterId)
            )
        )
        {
            if (client.ConnectionId != session.ConnectionId)
                _ = client.SendAsync(PacketType.CircleChatForwardNotify, forward, ct);
        }
    }
}
