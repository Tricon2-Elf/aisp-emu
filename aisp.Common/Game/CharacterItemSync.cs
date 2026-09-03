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
    /// Quantity written into recv_item_create when an unequipped copy returns to the bag.
    /// </summary>
    public const uint InventoryListNum = 1;

    public static uint ResolveObjId(IPlayerSession session) =>
        session.CharacterId != 0 ? session.CharacterId : 1u;

    public static Task SendInventoryBootstrapAsync(
        IPlayerSession session,
        Character character,
        CancellationToken ct
    )
    {
        var packets = new List<(PacketType Type, byte[] Payload)>
        {
            (PacketType.ItemGetListResponse, new ItemGetListResponse(0).ToBytes()),
        };
        AppendBootstrapPackets(packets, session, character);
        return session.SendAsync(packets, ct);
    }

    public static Task SendInventoryBootstrapAsync(
        IPlayerSession session,
        Character character,
        IEnumerable<(int ItemId, int Quantity)> storageItems,
        CancellationToken ct
    )
    {
        var packets = new List<(PacketType Type, byte[] Payload)>
        {
            (PacketType.ItemGetListResponse, new ItemGetListResponse(0).ToBytes()),
        };
        AppendBootstrapPackets(packets, session, character);
        AppendStorageBootstrapPackets(packets, storageItems);
        return session.SendAsync(packets, ct);
    }

    public static Task SendStorageBootstrapAsync(
        IPlayerSession session,
        IEnumerable<(int ItemId, int Quantity)> storageItems,
        CancellationToken ct
    )
    {
        var packets = new List<(PacketType Type, byte[] Payload)>();
        AppendStorageBootstrapPackets(packets, storageItems);
        return session.SendAsync(packets, ct);
    }

    /// <summary>
    /// Pushes a changed bag stack count, e.g. after discard/consume,
    /// recv_item_update_num while copies remain, recv_item_delete once the stack is gone.
    /// </summary>
    public static Task SendInventoryQuantityAsync(
        IPlayerSession session,
        int itemId,
        int remaining,
        CancellationToken ct
    )
    {
        var serialId = ResolveSerialId(itemId);
        if (remaining <= 0)
        {
            return session.SendAsync(
                PacketType.ItemDeleteNotify,
                new ItemDeleteNotify(PrimaryItemTablePlace, serialId).ToBytes(),
                ct
            );
        }

        return session.SendAsync(
            PacketType.ItemUpdateNumNotify,
            new ItemUpdateNumNotify(
                PrimaryItemTablePlace,
                serialId,
                (ushort)Math.Min(remaining, ushort.MaxValue)
            ).ToBytes(),
            ct
        );
    }

    public static Task SyncItemTableQuantityAsync(
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
            return session.SendAsync(
                PacketType.ItemDeleteNotify,
                new ItemDeleteNotify(place, serialId).ToBytes(),
                ct
            );
        }

        return SendItemTableEntryAsync(
            session,
            place,
            itemId,
            (ushort)Math.Min(quantity, ushort.MaxValue),
            ct
        );
    }

    public static Task SendBootstrapAsync(
        IPlayerSession session,
        Character character,
        CancellationToken ct
    )
    {
        var packets = new List<(PacketType Type, byte[] Payload)>();
        AppendBootstrapPackets(packets, session, character);
        return session.SendAsync(packets, ct);
    }

    public static Task SendInventoryItemAsync(
        IPlayerSession session,
        int itemId,
        ushort quantity,
        CancellationToken ct
    )
    {
        return SendPrimaryItemTableEntryAsync(session, itemId, quantity, ct);
    }

    /// <summary>
    /// Synchronizes the number of unplaced copies shown by the furniture UI.
    /// The client requires recv_item_delete when the count reaches zero because
    /// its 65531 event rebuilds the furniture slot list.
    /// </summary>
    public static Task SendFurnitureInventoryAvailabilityAsync(
        IPlayerSession session,
        int itemId,
        int quantity,
        CancellationToken ct
    )
    {
        var packets = new List<(PacketType Type, byte[] Payload)>(1);
        AppendFurnitureInventoryAvailability(packets, itemId, quantity);
        return session.SendAsync(packets, ct);
    }

    public static void AppendFurnitureInventoryAvailability(
        List<(PacketType Type, byte[] Payload)> packets,
        int itemId,
        int quantity
    )
    {
        var serialId = ResolveSerialId(itemId);
        if (quantity <= 0)
        {
            packets.Add(
                (
                    PacketType.ItemDeleteNotify,
                    new ItemDeleteNotify(PrimaryItemTablePlace, serialId).ToBytes()
                )
            );
            return;
        }

        packets.Add(
            BuildItemTableEntry(
                PrimaryItemTablePlace,
                itemId,
                (ushort)Math.Min(quantity, ushort.MaxValue)
            )
        );
    }

    public static Task SendUnequippedAsync(
        IPlayerSession session,
        uint objId,
        EquippedItemChange change,
        CancellationToken ct
    )
    {
        var packets = new List<(PacketType Type, byte[] Payload)>();
        AppendUnequippedPackets(packets, objId, change);
        return session.SendAsync(packets, ct);
    }

    public static Task SendEquippedAsync(
        IPlayerSession session,
        uint objId,
        EquippedItemChange change,
        CancellationToken ct
    )
    {
        return session.SendAsync(
            PacketType.ItemEquippedNotify,
            BuildEquippedNotify(objId, change).ToBytes(),
            ct
        );
    }

    public static Task SendReplaceChangesAsync(
        IPlayerSession session,
        EquipReplaceResult result,
        CancellationToken ct
    )
    {
        var objId = ResolveObjId(session);
        var packets = new List<(PacketType Type, byte[] Payload)>();

        foreach (var removed in result.Removed)
            AppendUnequippedPackets(packets, objId, removed);

        foreach (var added in result.Added)
            packets.Add(
                (PacketType.ItemEquippedNotify, BuildEquippedNotify(objId, added).ToBytes())
            );

        AppendInventoryCountPackets(packets, result.InventoryCountsByItemId);
        return session.SendAsync(packets, ct);
    }

    /// <summary>
    /// Syncs bag quantities only (no avatar equip/unequip notifies). Used when dressing
    /// Charadolls / Robos so removed clothes return to the owner's inventory UI.
    /// </summary>
    public static Task SendInventoryCountsAsync(
        IPlayerSession session,
        IReadOnlyDictionary<int, int> inventoryCountsByItemId,
        CancellationToken ct
    )
    {
        var packets = new List<(PacketType Type, byte[] Payload)>();
        AppendInventoryCountPackets(packets, inventoryCountsByItemId);
        return session.SendAsync(packets, ct);
    }

    private static void AppendBootstrapPackets(
        List<(PacketType Type, byte[] Payload)> packets,
        IPlayerSession session,
        Character character
    )
    {
        var objId = ResolveObjId(session);

        foreach (var stack in character.Inventory.OrderBy(i => i.ItemId))
        {
            if (stack.Quantity <= 0)
                continue;

            packets.Add(
                BuildItemTableEntry(PrimaryItemTablePlace, stack.ItemId, (ushort)stack.Quantity)
            );
        }

        foreach (var equip in character.Equipment.OrderBy(e => e.SlotIndex))
        {
            if (equip.ItemId == 0)
                continue;

            var serialId = ResolveSerialId(equip.ItemId);
            var socket = ItemEntityMapper.ResolveBodyspot(
                equip.ItemId,
                storedSocket: equip.Item?.Socket ?? 0,
                name: equip.Item?.Name
            );

            packets.Add(
                (
                    PacketType.ItemEquippedNotify,
                    new ItemEquippedNotify(objId, serialId, socket).ToBytes()
                )
            );
        }
    }

    private static void AppendStorageBootstrapPackets(
        List<(PacketType Type, byte[] Payload)> packets,
        IEnumerable<(int ItemId, int Quantity)> storageItems
    )
    {
        foreach (var (itemId, quantity) in storageItems.OrderBy(x => x.ItemId))
        {
            if (quantity <= 0)
                continue;

            packets.Add(
                BuildItemTableEntry(
                    StorageItemTablePlace,
                    itemId,
                    (ushort)Math.Min(quantity, ushort.MaxValue)
                )
            );
        }
    }

    private static void AppendUnequippedPackets(
        List<(PacketType Type, byte[] Payload)> packets,
        uint objId,
        EquippedItemChange change
    )
    {
        var serialId = ResolveSerialId(change.ItemId);
        packets.Add(
            (
                PacketType.ItemRemovedNotify,
                new ItemRemovedNotify(objId, serialId, change.SocketBit).ToBytes()
            )
        );
        packets.Add(
            BuildItemTableEntry(PrimaryItemTablePlace, change.ItemId, (ushort)InventoryListNum)
        );
    }

    private static void AppendInventoryCountPackets(
        List<(PacketType Type, byte[] Payload)> packets,
        IReadOnlyDictionary<int, int> inventoryCountsByItemId
    )
    {
        foreach (var (itemId, count) in inventoryCountsByItemId)
        {
            var clamped = count <= 0 ? (ushort)0 : (ushort)Math.Min(count, ushort.MaxValue);
            packets.Add(BuildItemTableEntry(PrimaryItemTablePlace, itemId, clamped));
        }
    }

    private static ItemEquippedNotify BuildEquippedNotify(uint objId, EquippedItemChange change)
    {
        var serialId = ResolveSerialId(change.ItemId);
        var socket =
            change.SocketBit != 0
                ? change.SocketBit
                : ItemEntityMapper.ResolveBodyspot(change.ItemId, name: change.ItemName);
        return new ItemEquippedNotify(objId, serialId, socket);
    }

    private static (PacketType Type, byte[] Payload) BuildItemTableEntry(
        uint place,
        int itemId,
        ushort quantity
    )
    {
        var serialId = ResolveSerialId(itemId);
        return (
            PacketType.ItemCreateNotify,
            new ItemCreateNotify(place, serialId, quantity, (uint)itemId).ToBytes()
        );
    }

    private static Task SendPrimaryItemTableEntryAsync(
        IPlayerSession session,
        int itemId,
        ushort quantity,
        CancellationToken ct
    ) => SendItemTableEntryAsync(session, PrimaryItemTablePlace, itemId, quantity, ct);

    private static Task SendItemTableEntryAsync(
        IPlayerSession session,
        uint place,
        int itemId,
        ushort quantity,
        CancellationToken ct
    )
    {
        var (type, payload) = BuildItemTableEntry(place, itemId, quantity);
        return session.SendAsync(type, payload, ct);
    }
}
