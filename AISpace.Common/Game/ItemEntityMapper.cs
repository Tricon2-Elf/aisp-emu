using AISpace.Common.DAL.Entities;
using AISpace.Network.Data;

namespace AISpace.Common.Game;

internal static class ItemEntityMapper
{
    private const uint ShoeSlotPrimary = 512;
    private const uint ShoeSlotSecondary = 256;

    /// <summary>
    /// Returns the bodyspot bitmask for item catalog data (Socket1/Socket2).
    /// Clothing categories (100–107) are always derived from item id/name.
    /// </summary>
    public static uint ResolveBodyspot(int itemId, int storedSocket = 0, string? name = null)
    {
        if (itemId is >= 10_000_000 and < 200_000_000)
        {
            var derived = DeriveClothingBodyspot(itemId, name);
            if (derived != 0)
                return derived;
        }

        if (storedSocket != 0)
            return (uint)storedSocket;

        return 0;
    }

    public static uint ResolveBodyspot(Item item) => ResolveBodyspot(item.Id, item.Socket, item.Name);

    public static uint ResolveBodyspot(uint itemId) => ResolveBodyspot((int)itemId);

    /// <summary>
    /// Socket sent in equip packets. Shoes use 0 so the client picks the mesh from the item
    /// catalog (same as default create-info equipment). Other clothing uses bodyspot for UI slots.
    /// </summary>
    public static uint ResolveEquipSocket(CharacterEquipSlot slot) =>
        slot.ItemId is >= 10_500_000 and < 10_600_000 ? 0 : ResolveBodyspot(slot.ItemId);

    /// <summary>
    /// UI slot dockets from the client wardrobe (CSV columns 6+ → bit index):
    /// coat=1, hat=2, jacket=4, shirt=8, skirt=16, pants=32, gloves=64, socks=128,
    /// shoe (primary)=512, shoe (secondary)=256.
    /// </summary>
    private static uint DeriveClothingBodyspot(int itemId, string? name)
    {
        return (itemId / 100_000) switch
        {
            100 => 2, // hat
            101 => 8, // shirt
            102 => ResolveLowerBodyBodyspot(itemId, name),
            103 => 64, // gloves
            104 => 128, // socks
            105 => ShoeSlotPrimary, // shoes (primary wardrobe slot)
            106 => 1024, // bra
            107 => 2048, // lower underwear
            _ => 0,
        };
    }

    private static uint ResolveLowerBodyBodyspot(int itemId, string? name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            if (name.Contains("スカート", StringComparison.Ordinal))
                return 16;
            if (
                name.Contains("パンツ", StringComparison.Ordinal)
                || name.Contains("ズボン", StringComparison.Ordinal)
                || name.Contains("男性用", StringComparison.Ordinal)
                || name.Contains("ショートパンツ", StringComparison.Ordinal)
                || name.Contains("カブリ", StringComparison.Ordinal)
            )
                return 32;
        }

        return itemId == 10200100 ? 32u : 16u;
    }

    public static ItemData ToItemBaseListData(Item item)
    {
        var id = (uint)item.Id;
        var socket = ResolveBodyspot(item);
        var iconId = (uint)item.IconId;
        var (socket1, socket2) = GetCatalogSockets(item.Id, socket);

        uint category = 1;
        if (socket == 2)
            category = 2;
        if (socket == 4)
            category = 8;
        if (socket == 8)
            category = 8;
        if (socket == 16)
            category = 4;

        return new ItemData
        {
            Key = id,
            SortedListPriority = id,
            ItemId = id,
            IconId = iconId,
            Name = item.Name,
            Socket1 = socket1,
            Socket2 = socket2,
            Category = category,
        };
    }

    private static (uint Socket1, uint Socket2) GetCatalogSockets(int itemId, uint socket)
    {
        if (itemId / 100_000 == 105)
            return (ShoeSlotPrimary, ShoeSlotSecondary);

        return (socket, socket);
    }
}
