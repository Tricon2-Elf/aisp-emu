using aisp.Common.DAL.Entities;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Game;

internal static class CharacterItemSync
{
    public const uint PrimaryItemTablePlace = 0;
    public const uint StorageItemTablePlace = 1;

    /// <summary>
    /// Client item tables are keyed by (place, serialId); serial must match ItemId.
    /// Decompiled send_item_move (sub_7969F0) routes place=0 to GlobalItemCount and
    /// place=1 to ItemCount. Inventory lives on place=0; account 倉庫 warehouse on place=1.
    /// </summary>
    public static uint ResolveSerialId(int itemId) => (uint)itemId;

    /// <summary>
    /// recv_item_update_list third field is list count/num.
    /// </summary>
    public const uint InventoryListNum = 1;

    public static uint ResolveObjId(IPlayerSession session) =>
        session.CharacterId != 0 ? session.CharacterId : 1u;

    public static async Task SendInventoryBootstrapAsync(
        IPlayerSession session,
        Character character,
        CancellationToken ct
    )
    {
        await session.SendAsync(
            PacketType.ItemGetListResponse,
            new ItemGetListResponse(0).ToBytes(),
            ct
        );
        await SendBootstrapAsync(session, character, ct);
    }

    public static async Task SendInventoryBootstrapAsync(
        IPlayerSession session,
        Character character,
        IEnumerable<(int ItemId, int Quantity)> storageItems,
        CancellationToken ct
    )
    {
        await session.SendAsync(
            PacketType.ItemGetListResponse,
            new ItemGetListResponse(0).ToBytes(),
            ct
        );
        await SendBootstrapAsync(session, character, ct);
        await SendStorageBootstrapAsync(session, storageItems, ct);
    }

    public static async Task SendStorageBootstrapAsync(
        IPlayerSession session,
        IEnumerable<(int ItemId, int Quantity)> storageItems,
        CancellationToken ct
    )
    {
        foreach (var (itemId, quantity) in storageItems.OrderBy(x => x.ItemId))
        {
            if (quantity <= 0)
                continue;

            await SendItemTableEntryAsync(
                session,
                StorageItemTablePlace,
                itemId,
                (ushort)Math.Min(quantity, ushort.MaxValue),
                ct
            );
        }
    }

    public static async Task SyncItemTableQuantityAsync(
        IPlayerSession session,
        uint place,
        int itemId,
        int quantity,
        CancellationToken ct
    )
    {
        var serialId = ResolveSerialId(itemId);
        if (quantity <= 0)
        {
            await session.SendAsync(
                PacketType.ItemDeleteNotify,
                new ItemDeleteNotify(place, serialId).ToBytes(),
                ct
            );
            return;
        }

        await SendItemTableEntryAsync(
            session,
            place,
            itemId,
            (ushort)Math.Min(quantity, ushort.MaxValue),
            ct
        );
    }

    public static async Task SendBootstrapAsync(
        IPlayerSession session,
        Character character,
        CancellationToken ct
    )
    {
        var objId = ResolveObjId(session);

        // Seed primary item-table entries first.
        foreach (var stack in character.Inventory.OrderBy(i => i.ItemId))
        {
            if (stack.Quantity <= 0)
                continue;

            await SendPrimaryItemTableEntryAsync(session, stack.ItemId, (ushort)stack.Quantity, ct);
        }

        foreach (var equip in character.Equipment.OrderBy(e => e.SlotIndex))
        {
            if (equip.ItemId == 0)
                continue;

            var serialId = ResolveSerialId(equip.ItemId);
            var socket = ItemEntityMapper.ResolveBodyspot(equip.ItemId, name: equip.Item?.Name);

            await session.SendAsync(
                PacketType.ItemEquippedNotify,
                new ItemEquippedNotify(objId, serialId, socket).ToBytes(),
                ct
            );
        }
    }

    public static async Task SendInventoryItemAsync(
        IPlayerSession session,
        int itemId,
        ushort quantity,
        CancellationToken ct
    )
    {
        await SendPrimaryItemTableEntryAsync(session, itemId, quantity, ct);
    }

    /// <summary>
    /// Synchronizes the number of unplaced copies shown by the furniture UI.
    /// The client requires recv_item_delete when the count reaches zero because
    /// its 65531 event rebuilds the furniture slot list. A zero-valued
    /// recv_item_update_list alone leaves a stale selectable slot behind.
    /// </summary>
    public static async Task SendFurnitureInventoryAvailabilityAsync(
        IPlayerSession session,
        int itemId,
        int quantity,
        CancellationToken ct
    )
    {
        var serialId = ResolveSerialId(itemId);
        if (quantity <= 0)
        {
            await session.SendAsync(
                PacketType.ItemDeleteNotify,
                new ItemDeleteNotify(PrimaryItemTablePlace, serialId).ToBytes(),
                ct
            );
            return;
        }

        await SendPrimaryItemTableEntryAsync(
            session,
            itemId,
            (ushort)Math.Min(quantity, ushort.MaxValue),
            ct
        );
    }

    private static async Task SendInventoryCountAsync(
        IPlayerSession session,
        int itemId,
        int count,
        CancellationToken ct
    )
    {
        var clamped = count <= 0 ? (ushort)0 : (ushort)Math.Min(count, ushort.MaxValue);
        await SendInventoryItemAsync(session, itemId, clamped, ct);
    }

    public static async Task SendUnequippedAsync(
        IPlayerSession session,
        uint objId,
        EquippedItemChange change,
        CancellationToken ct
    )
    {
        var serialId = ResolveSerialId(change.ItemId);

        await session.SendAsync(
            PacketType.ItemRemovedNotify,
            new ItemRemovedNotify(objId, serialId, change.SocketBit).ToBytes(),
            ct
        );
        await SendPrimaryItemTableEntryAsync(session, change.ItemId, (ushort)InventoryListNum, ct);
    }

    public static async Task SendEquippedAsync(
        IPlayerSession session,
        uint objId,
        EquippedItemChange change,
        CancellationToken ct
    )
    {
        var serialId = ResolveSerialId(change.ItemId);
        var socket =
            change.SocketBit != 0
                ? change.SocketBit
                : ItemEntityMapper.ResolveBodyspot(change.ItemId, name: change.ItemName);

        await session.SendAsync(
            PacketType.ItemEquippedNotify,
            new ItemEquippedNotify(objId, serialId, socket).ToBytes(),
            ct
        );
    }

    public static async Task SendReplaceChangesAsync(
        IPlayerSession session,
        EquipReplaceResult result,
        CancellationToken ct
    )
    {
        var objId = ResolveObjId(session);

        foreach (var removed in result.Removed)
            await SendUnequippedAsync(session, objId, removed, ct);

        foreach (var added in result.Added)
            await SendEquippedAsync(session, objId, added, ct);

        await SendInventoryCountsAsync(session, result.InventoryCountsByItemId, ct);
    }

    /// <summary>
    /// Syncs bag quantities only (no avatar equip/unequip notifies). Used when dressing
    /// Charadolls / Robos so removed clothes return to the owner's inventory UI.
    /// </summary>
    public static async Task SendInventoryCountsAsync(
        IPlayerSession session,
        IReadOnlyDictionary<int, int> inventoryCountsByItemId,
        CancellationToken ct
    )
    {
        foreach (var (itemId, count) in inventoryCountsByItemId)
            await SendInventoryCountAsync(session, itemId, count, ct);
    }

    private static async Task SendPrimaryItemTableEntryAsync(
        IPlayerSession session,
        int itemId,
        ushort quantity,
        CancellationToken ct
    ) => await SendItemTableEntryAsync(session, PrimaryItemTablePlace, itemId, quantity, ct);

    private static async Task SendItemTableEntryAsync(
        IPlayerSession session,
        uint place,
        int itemId,
        ushort quantity,
        CancellationToken ct
    )
    {
        var serialId = ResolveSerialId(itemId);
        await session.SendAsync(
            PacketType.ItemCreateNotify,
            new ItemCreateNotify(place, serialId, quantity, (uint)itemId).ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.ItemUpdateListNotify,
            new ItemUpdateListNotify(place, serialId, quantity).ToBytes(),
            ct
        );
    }
}
