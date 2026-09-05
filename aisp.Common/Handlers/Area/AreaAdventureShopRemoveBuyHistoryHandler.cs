using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

/// <summary>Hides one 購入履歴 entry. The copy stays downloadable through the download list.</summary>
public sealed class AreaAdventureShopRemoveBuyHistoryHandler(IAdventureShopRepository shop)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AdventureShopRemoveBuyHistoryRequest;
    public PacketType ResponseType => PacketType.AdventureShopRemoveBuyHistoryResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = AdventureScriptIdRequest.FromBytes(payload.Span);
        await shop.HideHistoryAsync(session.User?.Id ?? session.UserId, request.ScriptId, ct);
        await session.SendAsync(
            ResponseType,
            new AdventureScriptIdResponse(0, request.ScriptId).ToBytes(),
            ct
        );
    }
}

/// <summary>Clears the 購入履歴.</summary>
public sealed class AreaAdventureShopRemoveAllBuyHistoryHandler(IAdventureShopRepository shop)
    : PacketHandlerBase<
        AdventureShopRemoveAllBuyHistoryRequest,
        AdventureShopRemoveAllBuyHistoryResponse
    >,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.AdventureShopRemoveAllBuyHistoryRequest;
    public override PacketType ResponseType => PacketType.AdventureShopRemoveAllBuyHistoryResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<AdventureShopRemoveAllBuyHistoryResponse?> HandleAsync(
        AdventureShopRemoveAllBuyHistoryRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        await shop.HideAllHistoryAsync(session.User?.Id ?? session.UserId, ct);
        return new AdventureShopRemoveAllBuyHistoryResponse(0);
    }
}
