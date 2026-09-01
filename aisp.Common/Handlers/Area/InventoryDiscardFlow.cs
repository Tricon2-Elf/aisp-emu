using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

/// <summary>
/// Server side of throwing items away. Used by the bag's 捨てる option (send_item_discard, one
/// stack); written so the trashbox window flow (send_trashbox_discard_item, up to ten stacks)
/// can reuse it once that path is implemented and tested.
/// The client never touches its item table locally for either flow; it only shows the result of
/// the _r packet and relies on recv_item_update_num / recv_item_delete for the bag itself, so those are
/// sent here before the caller sends its _r.
/// </summary>
internal static class InventoryDiscardFlow
{
    public static async Task<bool> DiscardAsync(
        ICharacterRepository characterRepo,
        IPlayerSession session,
        IEnumerable<(uint SerialId, ushort Num)> stacks,
        ILogger logger,
        CancellationToken ct
    )
    {
        if (session.CharacterId == 0)
            return false;

        // The same serial may appear more than once in a trashbox request; discard the sum.
        var requested = new Dictionary<int, int>();
        foreach (var (serialId, num) in stacks)
        {
            if (num == 0 || serialId == 0 || serialId > int.MaxValue)
                return false;
            var itemId = (int)serialId;
            requested[itemId] = requested.GetValueOrDefault(itemId) + num;
        }
        if (requested.Count == 0)
            return false;

        var character = await characterRepo.GetByIdAsync((int)session.CharacterId, ct);
        if (character is null)
            return false;

        var planned = new List<(int ItemId, int Remove, int Remaining)>();
        foreach (var (itemId, remove) in requested)
        {
            var stack = character.Inventory.FirstOrDefault(i => i.ItemId == itemId);
            if (stack is null || stack.Quantity < remove)
            {
                logger.LogWarning(
                    "Discard refused for character {CharacterId}: item {ItemId} x{Remove} not in bag (has {Have})",
                    session.CharacterId,
                    itemId,
                    remove,
                    stack?.Quantity ?? 0
                );
                return false;
            }

            var remaining = stack.Quantity - remove;
            if (remaining == 0 && character.Equipment.Any(e => e.ItemId == itemId))
            {
                logger.LogWarning(
                    "Discard refused for character {CharacterId}: item {ItemId} is equipped",
                    session.CharacterId,
                    itemId
                );
                return false;
            }

            planned.Add((itemId, remove, remaining));
        }

        var ok = true;
        var applied = new List<(int ItemId, int Remaining)>();
        foreach (var (itemId, remove, remaining) in planned)
        {
            try
            {
                await characterRepo.RemoveInventoryAsync(
                    (int)session.CharacterId,
                    itemId,
                    remove,
                    ct
                );
                applied.Add((itemId, remaining));
            }
            catch (InvalidOperationException ex)
            {
                // Placed MyRoom furniture must stay owned; keep what was already discarded consistent.
                logger.LogWarning(
                    ex,
                    "Discard refused for character {CharacterId}: item {ItemId} is placed in MyRoom",
                    session.CharacterId,
                    itemId
                );
                ok = false;
                break;
            }
        }

        foreach (var (itemId, remaining) in applied)
            await CharacterItemSync.SendInventoryQuantityAsync(session, itemId, remaining, ct);

        if (applied.Count > 0)
            session.Character = await characterRepo.GetByIdAsync((int)session.CharacterId, ct);

        return ok;
    }
}
