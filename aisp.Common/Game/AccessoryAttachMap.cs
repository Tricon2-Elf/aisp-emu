using System.Numerics;
using aisp.Network.Data;

namespace aisp.Common.Game;

/// <summary>
/// The client paper-doll maps accessory catalog/notify sockets by bit index (1&lt;&lt;cell).
/// Seed JSON sockets are attach IDs, not those cells. Item id decides the cell; the overlap
/// bit is always 1&lt;&lt;cell for cells 12-29 (bits 0-11 are clothing).
/// Confirmed cells: 12 glasses, 13 wig, 14 necklace, 15 right hair ribbon, 16 hair ribbon,
/// 17 right earring, 19 right handbag, 24 left shoulder band, 26 left shoulder bag.
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

        if (prefix == 109)
            return CharacterEquipmentSlotIndex.Wig;

        if (prefix == 108)
            return Face108(itemId, seed);

        if (prefix == 117)
        {
            return seed == 14
                ? CharacterEquipmentSlotIndex.HairRibbon
                : CharacterEquipmentSlotIndex.Headband;
        }

        return seed switch
        {
            10 => CharacterEquipmentSlotIndex.Headband,
            12 => CharacterEquipmentSlotIndex.Necklace,
            14 => CharacterEquipmentSlotIndex.HairRibbon,
            15 => CharacterEquipmentSlotIndex.WristLeft,
            18 => CharacterEquipmentSlotIndex.Handheld,
            20 => CharacterEquipmentSlotIndex.WristPrimary,
            21 => CharacterEquipmentSlotIndex.WristRibbon,
            22 => CharacterEquipmentSlotIndex.Armband,
            23 => CharacterEquipmentSlotIndex.WristCharm,
            26 => CharacterEquipmentSlotIndex.Wings,
            27 => CharacterEquipmentSlotIndex.Tail,
            60 => CharacterEquipmentSlotIndex.Handheld,
            _ => CharacterEquipmentSlotIndex.Accessory,
        };
    }

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
