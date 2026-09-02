using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

/// <summary>
/// The client's verdict on its HTTP manuscript upload. Report is the XML parser's boolean result: 1 after
/// 「アップロードに成功しました！」 (verified live), so 1 puts the pending listing on sale (provided upload.php
/// actually stored the manuscript); anything else drops it so the work can be uploaded again.
/// </summary>
public sealed class AreaAdventureUploadRequestReportHandler(
    IAdventureShopRepository shop,
    ILogger<AreaAdventureUploadRequestReportHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public const uint NotListedResult = 1;

    /// <summary>The report value the client sends after a successful upload.</summary>
    public const uint UploadSucceededReport = 1;

    public PacketType RequestType => PacketType.AdventureUploadRequestReportRequest;
    public PacketType ResponseType => PacketType.AdventureUploadRequestReportResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = AdventureUploadRequestReportRequest.FromBytes(payload.Span);
        var userId = session.User?.Id ?? session.UserId;
        uint result;
        if (request.Report == UploadSucceededReport)
        {
            var listing = await shop.ConfirmUploadAsync(userId, request.ScriptId, ct);
            result = listing is null ? NotListedResult : 0;
            logger.LogInformation(
                "AdventureUploadRequestReport from user {UserId}: work {WorkId} script {ScriptId} reported ok -> {Outcome}",
                userId,
                request.WorkId,
                request.ScriptId,
                listing is null ? "no stored manuscript, not listed" : "listed"
            );
        }
        else
        {
            await shop.AbandonUploadAsync(userId, request.ScriptId, ct);
            result = 0;
            logger.LogInformation(
                "AdventureUploadRequestReport from user {UserId}: work {WorkId} script {ScriptId} reported failure {Report}; pending listing dropped",
                userId,
                request.WorkId,
                request.ScriptId,
                request.Report
            );
        }
        await session.SendAsync(
            ResponseType,
            new AdventureUploadRequestReportResponse(result, request.ScriptId).ToBytes(),
            ct
        );
    }
}
