using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Localisation;
using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

public class PostTalkHandler(
    SharedState state,
    IWordFilter wordFilter,
    ITextLocaliser localiser,
    IChatLogRepository chatLog
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.PostTalkRequest;
    public PacketType ResponseType => PacketType.PostTalkResponse;
    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var chatRequest = PostTalkRequest.FromBytes(payload.Span);
        if (wordFilter.ContainsBlockedWord(WordFilterLevel.NoSlurs, chatRequest.Message))
        {
            await chatLog.AddAsync(
                ChatLogCapture.FromSession(
                    session,
                    state,
                    ChatLogKind.Public,
                    chatRequest.Message,
                    chatRequest.DistID,
                    chatRequest.BalloonID,
                    rejected: true
                ),
                ct
            );
            await session.SendAsync(
                ResponseType,
                new PostTalkResponse(chatRequest.MessageID, 1).ToBytes(),
                ct
            );
            await SystemNotice.SendAsync(session, localiser.Get(session, L.Chat.SlurRejected), ct);
            return;
        }

        if (
            chatRequest.DistID == 0
            && state.TryTakePlacardComment(session.UserId, out var placardId)
        )
        {
            var placard = state.GetFriendLinkPlacard(placardId);
            var authorCharacterId = session.CharacterId;
            if (authorCharacterId == 0)
                authorCharacterId = state.GetAreaSessionByUserId(session.UserId)?.CharacterId ?? 0;
            var authorName =
                session.Character?.Name
                ?? state.GetAreaSessionByUserId(session.UserId)?.Character?.Name
                ?? string.Empty;

            var comment = state.AddFriendLinkPlacardComment(
                placardId,
                session.UserId,
                authorCharacterId,
                authorName,
                chatRequest.Message
            );
            if (comment is not null)
            {
                // Placard comments use the normal talk-post packet, so record them here
                // before this branch returns. DistId carries the placard ID for moderation.
                await chatLog.AddAsync(
                    ChatLogCapture.FromSession(
                        session,
                        state,
                        ChatLogKind.Placard,
                        chatRequest.Message,
                        distId: placardId,
                        balloonId: chatRequest.BalloonID
                    ),
                    ct
                );
            }
            await session.SendAsync(
                ResponseType,
                new PostTalkResponse(chatRequest.MessageID, comment is null ? 1u : 0u).ToBytes(),
                ct
            );

            if (comment is not null)
            {
                var notification = new NotifyPlacardCommentLog(
                    0,
                    placardId,
                    [new PlacardCommentLogEntry(comment.AuthorName, comment.Message)]
                ).ToBytes();
                var recipients = state
                    .GetServerClients(ServerType.Msg)
                    .Where(client =>
                        client.IsAuthenticated
                        && placard is not null
                        && client.UserId == placard.OwnerUserId
                    )
                    .Append(session)
                    .DistinctBy(client => client.ConnectionId);
                foreach (var recipient in recipients)
                    await recipient.SendAsync(PacketType.NotifyPlacardCommentLog, notification, ct);
            }
            return;
        }

        var response = new PostTalkResponse(chatRequest.MessageID, 0);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);

        var fromId = session.CharacterId;
        if (fromId == 0)
        {
            var areaSession = state.GetAreaSessionByUserId(session.UserId);
            fromId = areaSession?.CharacterId ?? 0;
        }

        await chatLog.AddAsync(
            ChatLogCapture.FromSession(
                session,
                state,
                ChatLogKind.Public,
                chatRequest.Message,
                chatRequest.DistID,
                chatRequest.BalloonID
            ),
            ct
        );

        var forwardPacket = new TalkForwardNotify(
            fromId,
            chatRequest.DistID,
            chatRequest.Message,
            chatRequest.BalloonID
        );
        byte[] broadcastData = forwardPacket.ToBytes();

        // Fire-and-forget: a hung TCP write to one Msg client must not stall the single
        // Msg dispatch loop (chat, logins, and every other Msg packet share that loop).
        foreach (var client in state.GetServerClients(ServerType.Msg))
        {
            if (client.IsAuthenticated && client.ConnectionId != session.ConnectionId)
                _ = client.SendAsync(PacketType.TalkForwardNotify, broadcastData, ct);
        }
    }
}
