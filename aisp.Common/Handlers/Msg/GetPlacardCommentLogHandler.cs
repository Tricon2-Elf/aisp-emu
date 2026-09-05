using aisp.Common.Game;
using aisp.Common.Localisation;
using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

/// <summary>Completes the placard interaction flow when no comments have been posted yet.</summary>
public sealed class GetPlacardCommentLogHandler(SharedState state, ITextLocaliser localiser)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.GetPlacardCommentLogRequest;
    public PacketType ResponseType => PacketType.GetPlacardCommentLogResponse;
    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = GetPlacardCommentLogRequest.FromBytes(payload.Span);
        var placard = state.GetFriendLinkPlacard(request.PlacardId);
        await session.SendAsync(ResponseType, new GetPlacardCommentLogResponse(0).ToBytes(), ct);

        if (placard is not null)
            state.BeginPlacardComment(session.UserId, request.PlacardId);

        var comments = placard?.GetComments() ?? [];
        IReadOnlyList<PlacardCommentLogEntry> entries =
            comments.Count == 0
                ?
                [
                    new PlacardCommentLogEntry(
                        string.Empty,
                        localiser.Get(session, L.FriendLink.NoComments)
                    ),
                ]
                :
                [
                    .. comments.Select(comment => new PlacardCommentLogEntry(
                        comment.AuthorName,
                        comment.Message
                    )),
                ];
        await session.SendAsync(
            PacketType.NotifyPlacardCommentLog,
            new NotifyPlacardCommentLog(0, request.PlacardId, entries).ToBytes(),
            ct
        );
    }
}
