using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

/// <summary>The account's discs currently on sale, for the upload window's アップロードドラマ list.</summary>
public sealed class AreaGetAdventureUploadListHandler(IAdventureShopRepository shop)
    : PacketHandlerBase<GetAdventureUploadListRequest, GetAdventureUploadListResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.GetAdventureUploadListRequest;
    public override PacketType ResponseType => PacketType.GetAdventureUploadListResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<GetAdventureUploadListResponse?> HandleAsync(
        GetAdventureUploadListRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var listings = await shop.GetUploadListAsync(session.User?.Id ?? session.UserId, ct);
        var records = listings
            .Select(l => new AdventureUploadListRecord
            {
                ScriptId = l.ScriptId,
                AuthorName = l.AuthorName,
                Title = l.Title,
                Price = l.Price,
                Comment = l.Comment,
                ContentsPublic = l.ContentsPublic ? (byte)1 : (byte)0,
                Genre = (uint)Math.Max(0, l.Genre),
                FileSize = l.ContentSize,
                Sales = (uint)Math.Max(0, l.SalesCount),
                Tags = AdventureShopCatalog.ToRecord(l).Tags,
                UploadedAt = AdventureShopCatalog.ToUnixSeconds(l.ListedAt ?? l.CreatedAt),
            })
            .ToList();
        return new GetAdventureUploadListResponse(0, records);
    }
}
