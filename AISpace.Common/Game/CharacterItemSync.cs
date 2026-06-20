using AISpace.Common.DAL.Entities;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Game;

internal static class CharacterItemSync
{
    public const uint EquipmentPlace = 0;
    public const uint InventoryPlace = 0;

    /// <summary>
    /// Client item table and wardrobe UI lists are keyed by SerialId (sub_47C610 / sub_48CB60).
    /// Serial must match ItemId so instances link to item_base_list metadata and tab slots.
    /// The same serial can hold both place=0 (equipped) and place=1 (inventory) counts.
    /// </summary>
    public static uint ResolveSerialId(int itemId) => (uint)itemId;

    /// <summary>
    /// recv_item_update_list third field is list count/num.
    /// Wardrobe/equip UI pulls from place=0 list entries in this client build.
    /// </summary>
    public const uint InventoryListNum = 1;

    public static uint ResolveObjId(IPlayerSession session) => session.CharacterId != 0 ? session.CharacterId : 1u;

    public static async Task SendInventoryBootstrapAsync(IPlayerSession session, Character character, CancellationToken ct)
    {
        await session.SendAsync(PacketType.ItemGetListResponse, new ItemGetListResponse(0).ToBytes(), ct);
        await SendBootstrapAsync(session, character, ct);
    }

    public static async Task SendBootstrapAsync(IPlayerSession session, Character character, CancellationToken ct)
    {
        var objId = ResolveObjId(session);
        var inventoryCounts = character.Inventory.Where(i => i.Quantity > 0).ToDictionary(i => i.ItemId, i => i.Quantity);

        // Inventory first — wardrobe tabs read the place=0 list in this client build.
        foreach (var stack in character.Inventory.OrderBy(i => i.ItemId))
        {
            if (stack.Quantity <= 0)
                continue;

            var serialId = ResolveSerialId(stack.ItemId);

            await session.SendAsync(
                PacketType.ItemCreateNotify,
                new ItemCreateNotify(InventoryPlace, serialId, (ushort)stack.Quantity, (uint)stack.ItemId).ToBytes(),
                ct
            );
            await session.SendAsync(
                PacketType.ItemUpdateListNotify,
                new ItemUpdateListNotify(InventoryPlace, serialId, (uint)stack.Quantity).ToBytes(),
                ct
            );
        }

        foreach (var equip in character.Equipment.OrderBy(e => e.SlotIndex))
        {
            if (equip.ItemId == 0)
                continue;

            var serialId = ResolveSerialId(equip.ItemId);
            var socket = ItemEntityMapper.ResolveBodyspot(equip.ItemId, name: equip.Item?.Name);

            // Avoid rewriting the same place/serial entry when inventory already created it.
            // Re-sending create with num=1 here can desync local counts and block unequip.
            if (!inventoryCounts.TryGetValue(equip.ItemId, out var inventoryCount) || inventoryCount <= 0)
            {
                await session.SendAsync(
                    PacketType.ItemCreateNotify,
                    new ItemCreateNotify(EquipmentPlace, serialId, 1, (uint)equip.ItemId).ToBytes(),
                    ct
                );
            }

            await session.SendAsync(PacketType.ItemEquippedNotify, new ItemEquippedNotify(objId, serialId, socket).ToBytes(), ct);
        }
    }

    public static async Task SendInventoryItemAsync(IPlayerSession session, int itemId, ushort quantity, CancellationToken ct)
    {
        var serialId = ResolveSerialId(itemId);

        await session.SendAsync(PacketType.ItemCreateNotify, new ItemCreateNotify(InventoryPlace, serialId, quantity, (uint)itemId).ToBytes(), ct);
        await session.SendAsync(PacketType.ItemUpdateListNotify, new ItemUpdateListNotify(InventoryPlace, serialId, quantity).ToBytes(), ct);
    }

    private static async Task SendInventoryCountAsync(IPlayerSession session, int itemId, int count, CancellationToken ct)
    {
        var clamped = count <= 0 ? (ushort)0 : (ushort)Math.Min(count, ushort.MaxValue);
        await SendInventoryItemAsync(session, itemId, clamped, ct);
    }

    public static async Task SendUnequippedAsync(IPlayerSession session, uint objId, EquippedItemChange change, CancellationToken ct)
    {
        var serialId = ResolveSerialId(change.ItemId);

        await session.SendAsync(PacketType.ItemRemovedNotify, new ItemRemovedNotify(objId, serialId, change.SocketBit).ToBytes(), ct);
        await session.SendAsync(PacketType.ItemCreateNotify, new ItemCreateNotify(InventoryPlace, serialId, 1, (uint)change.ItemId).ToBytes(), ct);
        await session.SendAsync(
            PacketType.ItemUpdateListNotify,
            new ItemUpdateListNotify(InventoryPlace, serialId, InventoryListNum).ToBytes(),
            ct
        );
    }

    public static async Task SendEquippedAsync(IPlayerSession session, uint objId, EquippedItemChange change, CancellationToken ct)
    {
        var serialId = ResolveSerialId(change.ItemId);
        var socket = change.SocketBit != 0 ? change.SocketBit : ItemEntityMapper.ResolveBodyspot(change.ItemId, name: change.ItemName);

        await session.SendAsync(PacketType.ItemEquippedNotify, new ItemEquippedNotify(objId, serialId, socket).ToBytes(), ct);
    }

    public static async Task SendReplaceChangesAsync(IPlayerSession session, EquipReplaceResult result, CancellationToken ct)
    {
        var objId = ResolveObjId(session);

        foreach (var removed in result.Removed)
            await SendUnequippedAsync(session, objId, removed, ct);

        foreach (var added in result.Added)
            await SendEquippedAsync(session, objId, added, ct);

        foreach (var (itemId, count) in result.InventoryCountsByItemId)
            await SendInventoryCountAsync(session, itemId, count, ct);
    }
}
