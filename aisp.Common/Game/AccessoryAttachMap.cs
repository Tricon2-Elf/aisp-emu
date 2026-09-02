using System.Numerics;
using aisp.Network.Data;

namespace aisp.Common.Game;

/// <summary>
/// The client paper-doll maps accessory catalog/notify sockets by bit index (1&lt;&lt;cell).
/// Seed JSON sockets are attach IDs, not those cells. Item-id prefix picks the family; the overlap
/// bit is always 1&lt;&lt;cell for cells 12-29 (bits 0-11 are clothing).
/// Confirmed cells: 12 glasses, 13 wig, 14 necklace, 15 right hair ribbon, 16 hair ribbon,
/// 17 right earring, 18 left earring, 19 right handbag, 24 left shoulder band, 26 left shoulder bag.
/// Prefixes: 108 face, 109 wig, 112 handheld/bags, 116 necklace, 117 hair ribbon, 118 mask.
/// </summary>
internal static class AccessoryAttachMap
{
    public const int GlassesItemIdMax = 10800062;

    public static uint ToSocketBit(int itemId, uint seedOrBit)
    {
        var slot = (byte)WindowSlot(itemId, seedOrBit);
        if (slot >= 12)
            return 1u << slot;

        // Cells 10-11 overlap bra/underwear bits; keep a high unique bit.
        return slot switch
        {
            10 => (uint)WardrobeSocketBit.Headband,
            11 => (uint)WardrobeSocketBit.Glasses,
            _ => 0,
        };
    }

    public static byte ToSlotIndex(int itemId, uint seedOrBit) =>
        (byte)WindowSlot(itemId, seedOrBit);

    private static CharacterEquipmentSlotIndex WindowSlot(int itemId, uint seedOrBit)
    {
        var prefix = itemId / 100_000;
        var seed = AttachSeed(seedOrBit);

        return prefix switch
        {
            109 => CharacterEquipmentSlotIndex.Wig,
            108 => Face108(itemId, seed),
            112 => BagOrHandheld(seed),
            114 => seed == 27 ? CharacterEquipmentSlotIndex.Tail : CharacterEquipmentSlotIndex.Wings,
            115 => Wrist115(seed),
            116 => CharacterEquipmentSlotIndex.Necklace,
            117 => Hair117(seed),
            118 => Mask118(seed),
            122 or 123 or 124 => CharacterEquipmentSlotIndex.Handheld,
            _ => SeedFallback(seed),
        };
    }

    private static CharacterEquipmentSlotIndex Hair117(uint seed) =>
        seed == 10 ? CharacterEquipmentSlotIndex.Headband : CharacterEquipmentSlotIndex.HairRibbon;

    private static CharacterEquipmentSlotIndex Mask118(uint seed) =>
        seed switch
        {
            50 => CharacterEquipmentSlotIndex.Headband,
            80 => CharacterEquipmentSlotIndex.KigurumiHead,
            _ => CharacterEquipmentSlotIndex.Glasses,
        };

    private static CharacterEquipmentSlotIndex BagOrHandheld(uint seed) =>
        seed switch
        {
            26 => CharacterEquipmentSlotIndex.LeftShoulderBag,
            24 => CharacterEquipmentSlotIndex.LeftShoulderBand,
            18 or 60 => CharacterEquipmentSlotIndex.Handheld,
            _ => CharacterEquipmentSlotIndex.RightHandbag,
        };

    private static CharacterEquipmentSlotIndex Wrist115(uint seed) =>
        seed switch
        {
            21 => CharacterEquipmentSlotIndex.WristRibbon,
            22 => CharacterEquipmentSlotIndex.Armband,
            23 => CharacterEquipmentSlotIndex.WristCharm,
            _ => CharacterEquipmentSlotIndex.WristPrimary,
        };

    private static CharacterEquipmentSlotIndex SeedFallback(uint seed) =>
        seed switch
        {
            10 => CharacterEquipmentSlotIndex.Headband,
            12 => CharacterEquipmentSlotIndex.Necklace,
            14 => CharacterEquipmentSlotIndex.HairRibbon,
            15 => CharacterEquipmentSlotIndex.WristLeft,
            18 or 60 => CharacterEquipmentSlotIndex.Handheld,
            19 => CharacterEquipmentSlotIndex.RightHandbag,
            20 => CharacterEquipmentSlotIndex.WristPrimary,
            21 => CharacterEquipmentSlotIndex.WristRibbon,
            22 => CharacterEquipmentSlotIndex.Armband,
            23 => CharacterEquipmentSlotIndex.WristCharm,
            24 => CharacterEquipmentSlotIndex.LeftShoulderBand,
            26 => CharacterEquipmentSlotIndex.Wings,
            27 => CharacterEquipmentSlotIndex.Tail,
            _ => CharacterEquipmentSlotIndex.Accessory,
        };

    private static CharacterEquipmentSlotIndex Face108(int itemId, uint seed)
    {
        if (itemId is >= 10800000 and <= GlassesItemIdMax)
            return CharacterEquipmentSlotIndex.Glasses;

        if (
            itemId is >= 10800070 and <= 10800079
            || itemId is >= 10800090 and <= 10800095
            || itemId is >= 10800110 and <= 10800121
        )
            return CharacterEquipmentSlotIndex.Glasses;

        if (itemId is >= 10800080 and <= 10800082 || itemId is >= 10800100 and <= 10800101)
            return CharacterEquipmentSlotIndex.LeftEarring;

        if (itemId == 10899999 || seed == 23)
            return CharacterEquipmentSlotIndex.WristCharm;

        if (itemId is >= 10800130 and < 10899999)
            return itemId % 2 == 1
                ? CharacterEquipmentSlotIndex.LeftEarring
                : CharacterEquipmentSlotIndex.RightEarring;

        return seed switch
        {
            15 => CharacterEquipmentSlotIndex.LeftEarring,
            16 => CharacterEquipmentSlotIndex.RightEarring,
            11 => CharacterEquipmentSlotIndex.Glasses,
            _ => CharacterEquipmentSlotIndex.Glasses,
        };
    }

    /// <summary>Ignore catalog one-hot bits so item id can re-home a previously wrong cell.</summary>
    private static uint AttachSeed(uint seedOrBit)
    {
        if (
            seedOrBit != 0
            && BitOperations.IsPow2(seedOrBit)
            && BitOperations.Log2(seedOrBit) >= 12
        )
            return 0;

        return seedOrBit;
    }
}
