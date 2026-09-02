using aisp.Common.Game;
using aisp.Common.Localisation;
using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

public class PostTalkHandler(SharedState state, IWordFilter wordFilter, ITextLocaliser localiser)
    : IPacketHandler,
        IRequiresAuthenticatedSession
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
            await session.SendAsync(
                ResponseType,
                new PostTalkResponse(chatRequest.MessageID, 1).ToBytes(),
                ct
            );
            await SystemNotice.SendAsync(session, localiser.Get(session, L.Chat.SlurRejected), ct);
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
