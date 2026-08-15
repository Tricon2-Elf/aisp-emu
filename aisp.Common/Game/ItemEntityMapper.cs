using aisp.Common.DAL.Entities;
using aisp.Network.Data;

namespace aisp.Common.Game;

internal enum WardrobeSocketBit : uint
{
    None = 0,
    Head = 1,
    UpperBodyLayer1 = 2,
    UpperBodyLayer2 = 4,
    UpperBodyLayer3 = 8,
    LowerBodyLayer1 = 16,
    LowerBodyLayer2 = 32,
    Hands = 64,
    Socks = 128,
    ShoesSecondary = 256,
    ShoesPrimary = 512,
    Bra = 1024,
    LowerUnderwear = 2048,
}

internal enum WardrobeCategoryId : uint
{
    None = 0,
    Hat = 0,
    Coat = 1,
    DressShirt = 2,
    TShirt = 3,
    Skirt = 4,
    Pants = 5,
    Gloves = 6,
    Socks = 7,
    Shoes = 8,
    Bra = 9,
    LowerUnderwear = 10,
    Accessory = 11,

    /// <summary>Client furniture tab group (sub_519E60 maps 12-14 → furniture).</summary>
    FurnitureFloor = 12,
    FurnitureWall = 13,
    FurnitureCeiling = 14,
}

internal static class ItemEntityMapper
{
    public static uint ResolveBodyspot(int itemId, int storedSocket = 0, string? name = null)
    {
        if (itemId is >= 10_000_000 and < 200_000_000)
        {
            var derived = DeriveClothingBodyspot(itemId, name);
            if (derived != (uint)WardrobeSocketBit.None)
                return derived;
        }

        if (storedSocket != 0)
            return (uint)storedSocket;

        return 0;
    }

    public static uint ResolveBodyspot(Item item) =>
        ResolveBodyspot(item.Id, item.Socket, item.Name);

    public static uint ResolveBodyspot(uint itemId) => ResolveBodyspot((int)itemId);

    public static uint ResolveEquipSocket(CharacterEquipSlot slot) =>
        slot.ItemId is >= 10_000_000 and < 200_000_000 ? 0 : ResolveBodyspot((int)slot.ItemId);

    private static uint DeriveClothingBodyspot(int itemId, string? name)
    {
        return (itemId / 100_000) switch
        {
            100 => (uint)WardrobeSocketBit.Head,
            101 => (uint)WardrobeSocketBit.UpperBodyLayer3,
            102 => ResolveLowerBodyBodyspot(itemId, name),
            103 => (uint)WardrobeSocketBit.Hands,
            104 => (uint)WardrobeSocketBit.Socks,
            105 => (uint)WardrobeSocketBit.ShoesPrimary,
            106 => (uint)WardrobeSocketBit.Bra,
            107 => (uint)WardrobeSocketBit.LowerUnderwear,
            _ => (uint)WardrobeSocketBit.None,
        };
    }

    private static uint ResolveLowerBodyBodyspot(int itemId, string? name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            if (name.Contains("スカート", StringComparison.Ordinal))
                return (uint)WardrobeSocketBit.LowerBodyLayer1;
            if (
                name.Contains("パンツ", StringComparison.Ordinal)
                || name.Contains("ズボン", StringComparison.Ordinal)
                || name.Contains("男性用", StringComparison.Ordinal)
                || name.Contains("ショートパンツ", StringComparison.Ordinal)
                || name.Contains("カブリ", StringComparison.Ordinal)
            )
                return (uint)WardrobeSocketBit.LowerBodyLayer2;
        }

        return itemId == 10200100
            ? (uint)WardrobeSocketBit.LowerBodyLayer2
            : (uint)WardrobeSocketBit.LowerBodyLayer1;
    }

    public static ItemData ToItemBaseListData(
        Item item,
        string? localisedName = null,
        string? localisedDescription = null,
        string? localisedLimitDescription = null
    )
    {
        const string itemDataDefaultDescription = "N/A";
        const string itemDataDefaultLimitDescription = "N/A";
        var id = (uint)item.Id;
        var iconId = (uint)item.IconId;
        var (socket1, socket2) = GetCatalogSockets(item.Id, ResolveBodyspot(item));
        var category = item.CatalogCategory is int persisted
            ? (uint)persisted
            : ResolveCatalogCategory(item);
        var limitMapKey = ResolveLimitMapKey(item.Id);

        return new ItemData
        {
            Key = id,
            SortedListPriority = id,
            ItemId = id,
            IconId = iconId,
            Name = localisedName ?? item.Name,
            Description = localisedDescription ?? itemDataDefaultDescription,
            LimitDesc = localisedLimitDescription ?? itemDataDefaultLimitDescription,
            Socket1 = socket1,
            Socket2 = socket2,
            Category = category,
            Flags = ResolveItemFlags(item.Id),
            MaxPossessionCount = (ushort)short.MaxValue,
            PlacementTypeId = limitMapKey,
        };
    }

    public static uint ResolveInventoryTabCategory(Item item) => ResolveCatalogCategory(item);

    public static uint ResolveInventoryTabCategory(int itemId, string? name = null) =>
        ResolveCatalogCategory(itemId, name, placementFlags: null);

    internal static uint ResolvePersistedCatalogCategory(
        int itemId,
        string? canonicalName,
        FurniturePlacementFlags? placementFlags
    ) => ResolveCatalogCategory(itemId, canonicalName, placementFlags);

    private static uint ResolveCatalogCategory(Item item) =>
        ResolveCatalogCategory(item.Id, item.Name, item.Furniture?.PlacementFlags);

    private static uint ResolveCatalogCategory(
        int itemId,
        string? name,
        FurniturePlacementFlags? placementFlags
    )
    {
        if (placementFlags is { } flags && flags != 0)
            return ResolveFurnitureCategory(flags);

        if (itemId is < 10_000_000 or >= 200_000_000)
            return (uint)WardrobeCategoryId.None;

        // Furniture catalog IDs are 11xxxxxx; without a Furniture row they still must
        // not fall into clothing category 0 (hat), or the wardrobe furniture tab stays empty.
        if (itemId / 100_000 >= 110)
            return (uint)WardrobeCategoryId.FurnitureFloor;

        if (
            !string.IsNullOrEmpty(name)
            && (
                name.Contains("コート", StringComparison.Ordinal)
                || name.Contains("アウター", StringComparison.Ordinal)
            )
        )
            return (uint)WardrobeCategoryId.Coat;

        return (itemId / 100_000) switch
        {
            100 => (uint)WardrobeCategoryId.Hat,
            101 => ResolveUpperBodyCategory(name),
            102 => ResolveLowerBodyCategory(itemId, name),
            103 => (uint)WardrobeCategoryId.Gloves,
            104 => (uint)WardrobeCategoryId.Socks,
            105 => (uint)WardrobeCategoryId.Shoes,
            106 => (uint)WardrobeCategoryId.Bra,
            107 => (uint)WardrobeCategoryId.LowerUnderwear,
            108 => (uint)WardrobeCategoryId.Accessory,
            _ => (uint)WardrobeCategoryId.None,
        };
    }

    private static uint ResolveFurnitureCategory(FurniturePlacementFlags flags)
    {
        if ((flags & FurniturePlacementFlags.Floor) != 0)
            return (uint)WardrobeCategoryId.FurnitureFloor;
        if ((flags & FurniturePlacementFlags.Wall) != 0)
            return (uint)WardrobeCategoryId.FurnitureWall;
        if ((flags & FurniturePlacementFlags.Ceiling) != 0)
            return (uint)WardrobeCategoryId.FurnitureCeiling;

        return (uint)WardrobeCategoryId.FurnitureFloor;
    }

    private static uint ResolveUpperBodyCategory(string? name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            if (
                name.Contains("Yシャツ", StringComparison.Ordinal)
                || name.Contains("ワイシャツ", StringComparison.Ordinal)
                || name.Contains("ブラウス", StringComparison.Ordinal)
            )
                return (uint)WardrobeCategoryId.DressShirt;
        }

        return (uint)WardrobeCategoryId.TShirt;
    }

    private static uint ResolveLowerBodyCategory(int itemId, string? name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            if (name.Contains("スカート", StringComparison.Ordinal))
                return (uint)WardrobeCategoryId.Skirt;
            if (
                name.Contains("パンツ", StringComparison.Ordinal)
                || name.Contains("ズボン", StringComparison.Ordinal)
                || name.Contains("男性用", StringComparison.Ordinal)
                || name.Contains("ショートパンツ", StringComparison.Ordinal)
                || name.Contains("カブリ", StringComparison.Ordinal)
            )
                return (uint)WardrobeCategoryId.Pants;
        }

        return itemId == 10200100 ? (uint)WardrobeCategoryId.Pants : (uint)WardrobeCategoryId.Skirt;
    }

    private static (uint Socket1, uint Socket2) GetCatalogSockets(int itemId, uint socket)
    {
        if (itemId / 100_000 == 105)
            return ((uint)WardrobeSocketBit.ShoesPrimary, (uint)WardrobeSocketBit.ShoesSecondary);

        return (socket, 0);
    }

    private static uint ResolveLimitMapKey(int itemId)
    {
        if (itemId is < 10_000_000 or >= 200_000_000)
            return (uint)itemId;

        var prefix = itemId / 100_000;
        return prefix switch
        {
            101 or 102 or 103 or 104 or 105 or 106 or 107 => (uint)prefix,
            _ => 200u,
        };
    }

    private static ItemFlags ResolveItemFlags(int itemId)
    {
        if (itemId is < 10_000_000 or >= 200_000_000)
            return ItemFlags.None;

        return (itemId / 100_000) switch
        {
            101 => ItemFlags.PermitsUnderwearTop,
            102 => ItemFlags.PermitsUnderwearBottom,
            106 => ItemFlags.PermitsUnderwearTop,
            107 => ItemFlags.PermitsUnderwearBottom,
            _ => ItemFlags.None,
        };
    }
}
