using AISpace.Network.Packets.Area;

namespace AISpace.Common.Game;

internal static class StarterShopCatalog
{
    public sealed record Entry(uint ItemId, uint NpsPrice, uint NiconicoPrice);

    // Keep this intentionally small for first-pass interoperability with the client shop flow.
    public static readonly IReadOnlyList<Entry> Items =
    [
        new(10100220, 50, 50), // male default top
        new(10200100, 50, 50), // male default bottom
        new(10400030, 50, 50), // male default shoes
        new(10500070, 50, 50), // male default accessory
        new(10100060, 50, 50), // female default top
        new(10200090, 50, 50), // female default bottom
        new(10400000, 50, 50), // female default shoes
        new(10500010, 50, 50), // female default accessory
    ];

    public static uint ResolvePrice(Entry entry, ShopPriceType priceType) =>
        priceType switch
        {
            ShopPriceType.NpsPoints => entry.NpsPrice,
            ShopPriceType.NiconicoPoints => entry.NiconicoPrice,
            _ => 0,
        };
}
