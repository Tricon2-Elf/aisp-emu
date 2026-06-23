using AISpace.Common.DAL;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public sealed class AreaShopBuyHandler(
    MainContext db,
    ICharacterRepository characterRepository,
    INpcRepository npcRepository,
    IShopRepository shopRepository,
    ILogger<AreaShopBuyHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.ShopBuyRequest;
    public PacketType ResponseType => PacketType.ShopBuyResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        if (session.CharacterId == 0 || session.User is null)
        {
            await session.SendAsync(ResponseType, new ShopBuyResponse(1, 0).ToBytes(), ct);
            return;
        }

        ShopBuyRequest request;
        try
        {
            request = ShopBuyRequest.FromBytes(payload.Span);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse ShopBuyRequest for character {CharacterId}", session.CharacterId);
            var total = (ulong)Math.Max(0, session.User.AiPoints);
            await session.SendAsync(ResponseType, new ShopBuyResponse(1, total).ToBytes(), ct);
            return;
        }

        if (request.PriceType is not ShopPriceType.AiPoints and not ShopPriceType.NicoPoints)
        {
            var total = (ulong)Math.Max(0, session.User.AiPoints);
            await session.SendAsync(ResponseType, new ShopBuyResponse(1, total).ToBytes(), ct);
            return;
        }

        var shop = await npcRepository.GetSingleActiveShopForMapAsync(session.MapId, ct);
        if (shop is null)
        {
            var total = request.PriceType == ShopPriceType.NicoPoints ? (ulong)Math.Max(0, session.User.NicoPoints) : (ulong)Math.Max(0, session.User.AiPoints);
            await session.SendAsync(ResponseType, new ShopBuyResponse(1, total).ToBytes(), ct);
            return;
        }

        var enabledShopItems = await shopRepository.GetEnabledItemsAsync(shop.Id, ct);
        var catalog = enabledShopItems.ToDictionary(x => (uint)x.ItemId, x => x);
        // Client send_shop_buy entries do not include an explicit quantity field.
        // Treat repeated item ids as repeated single-unit purchases.
        var mergedQuantities = new Dictionary<uint, uint>();
        foreach (var item in request.Items)
        {
            if (!catalog.ContainsKey(item.ItemId))
                continue;

            mergedQuantities[item.ItemId] = mergedQuantities.GetValueOrDefault(item.ItemId) + 1;
        }

        if (mergedQuantities.Count == 0)
        {
            var total = (ulong)Math.Max(0, session.User.AiPoints);
            await session.SendAsync(ResponseType, new ShopBuyResponse(1, total).ToBytes(), ct);
            return;
        }

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == session.User.Id, ct);
        if (user is null)
        {
            await session.SendAsync(ResponseType, new ShopBuyResponse(1, 0).ToBytes(), ct);
            return;
        }

        ulong totalCost = 0;
        foreach (var (itemId, quantity) in mergedQuantities)
        {
            var row = catalog[itemId];
            var unitPrice = request.PriceType == ShopPriceType.NicoPoints ? row.NicoPrice : row.AiPrice;
            // Reject zero-priced entries so catalog/config mistakes cannot grant free items.
            if (unitPrice == 0)
            {
                var total = request.PriceType == ShopPriceType.NicoPoints ? (ulong)Math.Max(0, session.User.NicoPoints) : (ulong)Math.Max(0, session.User.AiPoints);
                await session.SendAsync(ResponseType, new ShopBuyResponse(1, total).ToBytes(), ct);
                return;
            }

            totalCost += checked((ulong)unitPrice * quantity);
        }

        ulong currentBalance = request.PriceType switch
        {
            ShopPriceType.AiPoints => (ulong)Math.Max(0, user.AiPoints),
            ShopPriceType.NicoPoints => (ulong)Math.Max(0, user.NicoPoints),
            _ => 0,
        };

        if (currentBalance < totalCost)
        {
            await session.SendAsync(ResponseType, new ShopBuyResponse(1, currentBalance).ToBytes(), ct);
            return;
        }

        foreach (var (itemId, quantity) in mergedQuantities)
        {
            await characterRepository.AddInventoryAsync((int)session.CharacterId, (int)itemId, checked((int)quantity), ct);
        }

        var updatedBalance = checked((long)(currentBalance - totalCost));
        switch (request.PriceType)
        {
            case ShopPriceType.AiPoints:
                user.AiPoints = updatedBalance;
                session.User.AiPoints = user.AiPoints;
                break;
            case ShopPriceType.NicoPoints:
                user.NicoPoints = updatedBalance;
                session.User.NicoPoints = user.NicoPoints;
                break;
        }

        await db.SaveChangesAsync(ct);
        await session.SendAsync(ResponseType, new ShopBuyResponse(0, (ulong)updatedBalance).ToBytes(), ct);
        if (request.PriceType == ShopPriceType.AiPoints)
        {
            await session.SendAsync(PacketType.MoneyUpdatedAipoint, new MoneyUpdatedAipointNotify((ulong)Math.Max(0, user.AiPoints)).ToBytes(), ct);
        }
        if (request.PriceType == ShopPriceType.NicoPoints)
        {
            await session.SendAsync(
                PacketType.MoneyUpdatedNicopoint,
                new MoneyUpdatedNicopointNotify((ulong)Math.Max(0, user.NicoPoints)).ToBytes(),
                ct
            );
        }

        var refreshedCharacter = await characterRepository.GetByIdAsync((int)session.CharacterId, ct);
        if (refreshedCharacter is not null)
        {
            session.Character = refreshedCharacter;
            await CharacterItemSync.SendInventoryBootstrapAsync(session, refreshedCharacter, ct);
        }
    }
}
