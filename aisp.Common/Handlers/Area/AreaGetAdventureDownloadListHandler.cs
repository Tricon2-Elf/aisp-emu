using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

/// <summary>The discs the account holds copies of (bought and not removed from the download list).</summary>
public sealed class AreaGetAdventureDownloadListHandler(IAdventureShopRepository shop)
    : PacketHandlerBase<GetAdventureDownloadListRequest, GetAdventureDownloadListResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.GetAdventureDownloadListRequest;
    public override PacketType ResponseType => PacketType.GetAdventureDownloadListResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<GetAdventureDownloadListResponse?> HandleAsync(
        GetAdventureDownloadListRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var purchases = await shop.GetDownloadListAsync(session.User?.Id ?? session.UserId, ct);
        var records = purchases
            .Select(p => new AdventureDownloadListRecord(
                p.ScriptId,
                AdventureShopCatalog.ToUnixSeconds(p.PurchasedAt),
                (uint)Math.Max(0, p.Listing.Pages),
                p.Listing.ContentsPublic ? (byte)1 : (byte)0
            ))
            .ToList();
        return new GetAdventureDownloadListResponse(0, records);
    }
}
