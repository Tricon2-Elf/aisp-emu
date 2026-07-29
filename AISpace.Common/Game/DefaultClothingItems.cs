namespace AISpace.Common.Game;

internal static class DefaultClothingItems
{
    public static readonly IReadOnlyList<int> Male = [10100220, 10200100, 10400030, 10500070];

    public static readonly IReadOnlyList<int> Female = [10100060, 10200090, 10400000, 10500010];

    public static IReadOnlyList<int> ForGender(int gender) => gender == 1 ? Male : Female;

    /// <summary>
    /// Male lower underwear (107*) is equipped-only for the wardrobe preview curtain; it has no wardrobe tab.
    /// </summary>
    public static bool IsEquippedOnlyItem(int itemId) => itemId / 100_000 == 107;

    public static IEnumerable<int> WardrobeInventoryForGender(int gender) =>
        ForGender(gender).Where(id => !IsEquippedOnlyItem(id));
}
