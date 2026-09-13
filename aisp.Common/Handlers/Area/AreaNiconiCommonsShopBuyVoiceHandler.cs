using aisp.Common.DAL;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public sealed class AreaNiconiCommonsShopBuyVoiceHandler(
    MainContext db,
    ICharacterRepository characters,
    ILogger<AreaNiconiCommonsShopBuyVoiceHandler> logger,
    DramaCatalog catalog
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.NiconiCommonsShopBuyVoiceRequest;
    public PacketType ResponseType => PacketType.NiconiCommonsShopBuyResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = NiconiCommonsShopBuyVoiceRequest.FromBytes(payload.Span);
        var product = await catalog.FindOfferAsync(session.ActiveShopId, 1, request.Id, ct);
        if (product is null)
        {
            logger.LogWarning(
                "NiconiCommonsShopBuyVoice unknown id {Id} from character {CharacterId}",
                request.Id,
                session.CharacterId
            );
            await NiconiCommonsShopPurchase.SendRefusalAsync(session, ct);
            return;
        }

        if (
            !await NiconiCommonsShopPurchase.TryChargeAndGrantBagItemAsync(
                db,
                session,
                product.Offer,
                request.Extra,
                product.ItemId,
                ct
            )
        )
        {
            await NiconiCommonsShopPurchase.SendRefusalAsync(session, ct);
            return;
        }

        await NiconiCommonsShopPurchase.SendPaidAsync(
            characters,
            session,
            PacketType.UccVoiceObtainNotify,
            new UccVoiceObtainNotify(request.Id).ToBytes(),
            ct
        );
    }
}
