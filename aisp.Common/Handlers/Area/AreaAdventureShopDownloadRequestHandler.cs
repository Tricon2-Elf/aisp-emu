using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

/// <summary>Re-download from the 購入履歴: hands out a download ticket when the player bought or wrote the disc.</summary>
public sealed class AreaAdventureShopDownloadRequestHandler(
    IAdventureShopRepository shop,
    ILogger<AreaAdventureShopDownloadRequestHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public const uint NotEntitledResult = 1;

    public PacketType RequestType => PacketType.AdventureShopDownloadRequestRequest;
    public PacketType ResponseType => PacketType.AdventureShopDownloadRequestResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = AdventureShopDownloadRequestRequest.FromBytes(payload.Span);
        var userId = session.User?.Id ?? session.UserId;
        var ticket = await shop.IssueDownloadTicketAsync(userId, request.ScriptId, ct);
        if (ticket is null)
            logger.LogWarning(
                "AdventureShopDownloadRequest from user {UserId}: script {ScriptId} refused (not bought, not the author, or not listed)",
                userId,
                request.ScriptId
            );
        await session.SendAsync(
            ResponseType,
            new AdventureShopDownloadRequestResponse(
                ticket is null ? NotEntitledResult : 0u,
                request.ScriptId,
                ticket ?? ""
            ).ToBytes(),
            ct
        );
    }
}
