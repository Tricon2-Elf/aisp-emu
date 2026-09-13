using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.Game;

internal static class NiconiCommonsShopPurchase
{
    public static async Task SendRefusalAsync(IPlayerSession session, CancellationToken ct)
    {
        var remained = (ulong)Math.Max(0, session.User?.AiPoints ?? 0);
        await session.SendAsync(
            PacketType.NiconiCommonsShopBuyResponse,
            new NiconiCommonsShopBuyResponse(1, remained).ToBytes(),
            ct
        );
    }

    /// <summary>
    /// Charge the dere price and put the bag item in the character's bag in one SaveChanges,
    /// so a failure cannot leave the item granted without the charge or the other way round.
    /// </summary>
    public static async Task<bool> TryChargeAndGrantBagItemAsync(
        MainContext db,
        IPlayerSession session,
        NiconiCommonsShopItemRecord row,
        byte currency,
        int bagItemId,
        CancellationToken ct
    )
    {
        // These offers are D-only (NP=0). Reject disabled and unknown currency selectors.
        if (currency != 0 || row.AiPrice == 0 || session.CharacterId == 0 || session.User is null)
            return false;

        var characterId = checked((int)session.CharacterId);
        if (!await db.Characters.AnyAsync(c => c.Id == characterId, ct))
            return false;

        var stack = await db.CharacterInventories.SingleOrDefaultAsync(
            x => x.CharacterId == characterId && x.ItemId == bagItemId,
            ct
        );
        if (stack is { Quantity: > 0 })
            return false;

        var price = row.AiPrice;
        var user = await db.Users.SingleAsync(u => u.Id == session.User.Id, ct);
        if (user.AiPoints < price)
            return false;

        if (!await db.Items.AnyAsync(i => i.Id == bagItemId, ct))
            return false;

        if (stack is null)
            db.CharacterInventories.Add(
                new CharacterInventory
                {
                    CharacterId = characterId,
                    ItemId = bagItemId,
                    Quantity = 1,
                }
            );
        else
            stack.Quantity = 1;

        user.AiPoints -= price;
        await db.SaveChangesAsync(ct);
        session.User.AiPoints = user.AiPoints;
        return true;
    }

    public static async Task SendPaidAsync(
        ICharacterRepository characters,
        IPlayerSession session,
        PacketType obtainType,
        byte[] obtainPayload,
        CancellationToken ct
    )
    {
        var remained = (ulong)Math.Max(0, session.User?.AiPoints ?? 0);
        await session.SendAsync(
            PacketType.NiconiCommonsShopBuyResponse,
            new NiconiCommonsShopBuyResponse(0, remained).ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.MoneyUpdatedAipoint,
            new MoneyUpdatedAipointNotify(remained).ToBytes(),
            ct
        );
        await session.SendAsync(obtainType, obtainPayload, ct);

        var refreshed = await characters.GetByIdAsync((int)session.CharacterId, ct);
        if (refreshed is not null)
        {
            session.Character = refreshed;
            await CharacterItemSync.SendInventoryBootstrapAsync(session, refreshed, ct);
        }
    }
}
