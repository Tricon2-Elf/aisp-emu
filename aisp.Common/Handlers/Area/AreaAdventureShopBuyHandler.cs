using aisp.Common.Config;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace aisp.Common.Handlers.Area;

/// <summary>
/// Buying a drama disc. A successful purchase answers the new 購入履歴 row, the purse update (デレ, the in-game currency) and the buy
/// acknowledgement; the client then asks for the download itself (send_adventure_shop_download_request right
/// after the acknowledgement, seen live) and fetches the texts from download.php. Failures answer the
/// acknowledgement alone with a non-zero code; the client shows it in its error dialog.
/// </summary>
public sealed class AreaAdventureShopBuyHandler(
    IAdventureShopRepository shop,
    IOptions<ServerOptions> serverOptions,
    ILogger<AreaAdventureShopBuyHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AdventureShopBuyRequest;
    public PacketType ResponseType => PacketType.AdventureShopBuyResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = AdventureShopBuyRequest.FromBytes(payload.Span);
        var userId = session.User?.Id ?? session.UserId;
        if (request.PriceType != AdventureShopBuyRequest.AiPointsPriceType)
        {
            logger.LogWarning(
                "AdventureShopBuy from user {UserId}: script {ScriptId} with price type {PriceType}; only デレ is accepted",
                userId,
                request.ScriptId,
                request.PriceType
            );
            await session.SendAsync(
                ResponseType,
                new AdventureShopBuyResponse((uint)AdventureBuyOutcome.PriceMismatch).ToBytes(),
                ct
            );
            return;
        }

        var result = await shop.BuyAsync(
            userId,
            (int)session.CharacterId,
            request.ScriptId,
            request.Price,
            serverOptions.Value.AdventureUploadRatePercent,
            ct
        );
        if (result.Outcome != AdventureBuyOutcome.Bought || result.Purchase is null)
        {
            logger.LogInformation(
                "AdventureShopBuy from user {UserId}: script {ScriptId} for {Price} refused: {Outcome}",
                userId,
                request.ScriptId,
                request.Price,
                result.Outcome
            );
            await session.SendAsync(
                ResponseType,
                new AdventureShopBuyResponse((uint)result.Outcome).ToBytes(),
                ct
            );
            return;
        }

        if (session.User is not null)
            session.User.AiPoints = result.AiPoints;
        logger.LogInformation(
            "AdventureShopBuy from user {UserId}: bought script {ScriptId} \"{Title}\" for {Price} デレ; {AiPoints} left",
            userId,
            request.ScriptId,
            result.Purchase.Listing.Title,
            result.Purchase.Price,
            result.AiPoints
        );

        await session.SendAsync(
            PacketType.AdventureShopAddedBuyHistoryNotify,
            new AdventureShopAddedBuyHistoryNotify(
                AdventureShopCatalog.ToHistoryRow(result.Purchase)
            ).ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.MoneyUpdatedAipoint,
            new MoneyUpdatedAipointNotify((ulong)Math.Max(0, result.AiPoints)).ToBytes(),
            ct
        );
        await session.SendAsync(ResponseType, new AdventureShopBuyResponse(0).ToBytes(), ct);
    }
}
