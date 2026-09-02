using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

/// <summary>
/// Genre tab, sort combo or page combo in the drama disc shop window. The page goes out as
/// recv_adventure_shop_item first; the 4-byte recv_adventure_shop_genre_search_r after it is what releases the
/// window (it refreshes the lineup from whatever page it holds at that moment).
/// </summary>
public sealed class AreaAdventureShopGenreSearchHandler(AdventureShopCatalog catalog)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AdventureShopGenreSearchRequest;
    public PacketType ResponseType => PacketType.AdventureShopGenreSearchResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = AdventureShopGenreSearchRequest.FromBytes(payload.Span);
        var page = await catalog.BuildPageAsync(request, ct);
        await session.SendAsync(PacketType.AdventureShopItemNotify, page.ToBytes(), ct);
        await session.SendAsync(ResponseType, new AdventureShopGenreSearchResponse().ToBytes(), ct);
    }
}
