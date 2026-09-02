using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

/// <summary>
/// Drama upload request (the 説明事項 window's 同意する). The listing store, the HTTP upload endpoint and the
/// ticket handshake are not implemented yet, so this refuses the upload with a result the client shows as an
/// error dialog instead of leaving the window waiting forever. A success reply (result 0 + ticket) would make
/// the client POST the manuscript to the upload.php host from connection.txt.
/// </summary>
public sealed class AreaAdventureUploadRequestHandler(
    ILogger<AreaAdventureUploadRequestHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    /// <summary>Non-zero result; the client shows its generic drama error dialog for it.</summary>
    public const uint UploadUnavailableResult = 1;

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
        logger.LogInformation(
            "AdventureUploadRequest from user {UserId}: work {WorkId} \"{Title}\" genre {Genre} by \"{Author}\" price {Price} publish {Publish} size {ContentSize}; uploads are not available, refusing",
            session.UserId,
            request.WorkId,
            request.Title,
            request.Genre,
            request.AuthorName,
            request.Price,
            request.Publish,
            request.ContentSize
        );
        await session.SendAsync(
            ResponseType,
            new AdventureUploadRequestResponse(UploadUnavailableResult, request.WorkId).ToBytes(),
            ct
        );
    }
}
