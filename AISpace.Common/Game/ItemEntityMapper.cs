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
        var iconId = (uint)item.IconId;
        var (socket1, socket2) = GetCatalogSockets(item.Id, ResolveBodyspot(item));
        var category = ResolveCatalogCategory(item.Id, item.Name);
        var limitMapKey = ResolveLimitMapKey(item.Id);

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
            _0x044c = limitMapKey,
        };
    }

    /// <summary>
    /// Wardrobe inventory tab category (item_base_t dword_74 / category_skilleq20).
    /// Matches client CSV clothing types: shirt=3, skirt=4, pants=5, socks=7, shoes=8, bra=9, gloves=10, hat=11.
    /// </summary>
    private static uint ResolveCatalogCategory(int itemId, string? name)
    {
        if (itemId is < 10_000_000 or >= 200_000_000)
            return 0;

        if (!string.IsNullOrEmpty(name) && (name.Contains("コート", StringComparison.Ordinal) || name.Contains("アウター", StringComparison.Ordinal)))
            return 1;

        return (itemId / 100_000) switch
        {
            100 => 11, // hat
            101 => 3, // shirt
            102 => ResolveLowerBodyCategory(itemId, name),
            103 => 10, // gloves
            104 => 7, // socks
            105 => 8, // shoes
            106 => 9, // bra
            107 => 16, // lower underwear
            _ => 0,
        };
    }

    private static uint ResolveLowerBodyCategory(int itemId, string? name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            if (name.Contains("スカート", StringComparison.Ordinal))
                return 4;
            if (
                name.Contains("パンツ", StringComparison.Ordinal)
                || name.Contains("ズボン", StringComparison.Ordinal)
                || name.Contains("男性用", StringComparison.Ordinal)
                || name.Contains("ショートパンツ", StringComparison.Ordinal)
                || name.Contains("カブリ", StringComparison.Ordinal)
            )
                return 5;
        }

        return itemId == 10200100 ? 5u : 4u;
    }

    private static (uint Socket1, uint Socket2) GetCatalogSockets(int itemId, uint socket)
    {
        if (itemId / 100_000 == 105)
            return (ShoeSlotPrimary, ShoeSlotSecondary);

        // Socket2 is an alternate bodyspot; 0 means single-slot equip (no picker dialog).
        return (socket, 0);
    }

    /// <summary>
    /// Key into the client item-limit map (item_base_t dword_44c → dword_74_unk_map_idx).
    /// Zero fails sub_406E60 and blocks wardrobe re-equip. Clothing prefixes match default client entries (101–106).
    /// </summary>
    private static uint ResolveLimitMapKey(int itemId) =>
        itemId is >= 10_000_000 and < 200_000_000 ? (uint)(itemId / 100_000) : (uint)itemId;
}
