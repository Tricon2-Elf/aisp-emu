using aisp.Common.DAL;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public sealed class AreaNiconiCommonsShopBuyCommonsHandler(
    MainContext db,
    ICharacterRepository characters,
    ILogger<AreaNiconiCommonsShopBuyCommonsHandler> logger,
    DramaCatalog catalog
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.NiconiCommonsShopBuyCommonsRequest;
    public PacketType ResponseType => PacketType.NiconiCommonsShopBuyResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = NiconiCommonsShopBuyCommonsRequest.FromBytes(payload.Span);
        var product =
            await catalog.FindOfferAsync(session.ActiveShopId, 2, request.Id, ct)
            ?? await catalog.FindOfferAsync(session.ActiveShopId, 3, request.Id, ct);
        if (product is null)
        {
            logger.LogWarning(
                "NiconiCommonsShopBuyCommons unknown id {Id} from character {CharacterId}",
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
            PacketType.NiconiCommonsObtainNotify,
            new NiconiCommonsObtainNotify(request.Id).ToBytes(),
            ct
        );
    }
}
