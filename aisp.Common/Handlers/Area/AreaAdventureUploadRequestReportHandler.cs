using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

/// <summary>
/// The client's verdict on its HTTP manuscript upload. Nothing is stored yet (uploads are refused upstream),
/// so this only acknowledges; once uploads exist, report 0 is where the work gets its Uploaded flag.
/// </summary>
public sealed class AreaAdventureUploadRequestReportHandler(
    ILogger<AreaAdventureUploadRequestReportHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
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
        logger.LogInformation(
            "AdventureUploadRequestReport from user {UserId}: work {WorkId} script {ScriptId} report {Report}",
            session.UserId,
            request.WorkId,
            request.ScriptId,
            request.Report
        );
        await session.SendAsync(
            ResponseType,
            new AdventureUploadRequestReportResponse(0, request.ScriptId).ToBytes(),
            ct
        );
    }
}
