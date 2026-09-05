using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

/// <summary>
/// Drama upload request (the 説明事項 window's 同意する). Registers a pending listing for the work and hands the
/// client a one-time ticket; on result 0 the client POSTs the manuscript to upload.php with that ticket and then
/// reports the outcome with send_adventure_upload_request_report, which is where the listing goes on sale.
/// </summary>
public sealed class AreaAdventureUploadRequestHandler(
    IAdventureShopRepository shop,
    ILogger<AreaAdventureUploadRequestHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    /// <summary>Non-zero result; the client shows its generic drama error dialog for it.</summary>
    public const uint RefusedResult = 1;

    public PacketType RequestType => PacketType.AdventureUploadRequestRequest;
    public PacketType ResponseType => PacketType.AdventureUploadRequestResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = AdventureUploadRequestRequest.FromBytes(payload.Span);
        var userId = session.User?.Id ?? session.UserId;
        var draft = new AdventureListingDraft(
            request.Title,
            request.AuthorName,
            (int)Math.Min(request.Genre, int.MaxValue),
            request.Comment,
            request.Price,
            request.ContentsPublic != 0,
            request.ContentSize
        );
        var started =
            request.Price >= 0 && request.ContentSize >= 0
                ? await shop.BeginUploadAsync(
                    userId,
                    (int)session.CharacterId,
                    request.WorkId,
                    draft,
                    ct
                )
                : null;
        if (started is null)
        {
            logger.LogWarning(
                "AdventureUploadRequest from user {UserId}: work {WorkId} \"{Title}\" refused (unknown work or bad values)",
                userId,
                request.WorkId,
                request.Title
            );
            await session.SendAsync(
                ResponseType,
                new AdventureUploadRequestResponse(RefusedResult, request.WorkId).ToBytes(),
                ct
            );
            return;
        }

        var (listing, ticket) = started.Value;
        logger.LogInformation(
            "AdventureUploadRequest from user {UserId}: work {WorkId} \"{Title}\" genre {Genre} by \"{Author}\" price {Price} contents public {ContentsPublic} size {ContentSize} -> script {ScriptId}, awaiting upload.php",
            userId,
            request.WorkId,
            request.Title,
            request.Genre,
            request.AuthorName,
            request.Price,
            request.ContentsPublic,
            request.ContentSize,
            listing.ScriptId
        );
        await session.SendAsync(
            ResponseType,
            new AdventureUploadRequestResponse(
                0,
                request.WorkId,
                listing.ScriptId,
                ticket
            ).ToBytes(),
            ct
        );
    }
}
