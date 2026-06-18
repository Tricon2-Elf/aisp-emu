using AISpace.Common.DAL.Entities;
using AISpace.Network.Data;

namespace AISpace.Common.Game;

internal static class ItemEntityMapper
{
    /// <summary>
    /// Returns the bodyspot bitmask for an item. Clothing categories (100–107) are always
    /// derived from item id/name because the client matches UI slots with (slotDocket &amp; socket) != 0.
    /// Accessories and other items use the stored DB/seed socket when present.
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
    /// UI slot dockets from the client wardrobe (CSV columns 6+ → bit index):
    /// coat=1, hat=2, jacket=4, shirt=8, skirt=16, pants=32, gloves=64, socks=128, shoes=256.
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
            105 => 256, // shoes
            106 => 512, // bra
            107 => 1024, // lower underwear
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
            Socket1 = socket,
            Socket2 = socket,
            Category = category,
        };
    }
}
