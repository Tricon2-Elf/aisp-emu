using System.Numerics;
using aisp.Network.Data;

namespace aisp.Common.Game;

/// <summary>
/// The client paper-doll maps accessory catalog/notify sockets by bit index (1&lt;&lt;cell).
/// Seed JSON sockets are attach IDs, not those cells. Item-id prefix picks the family; the overlap
/// bit is always 1&lt;&lt;cell for cells 12-29 (bits 0-11 are clothing).
/// Confirmed cells: 12 glasses, 13 wig, 14 necklace, 15 right hair ribbon, 16 hair ribbon,
/// 17 right earring, 18 left earring, 19 right handbag, 24 left shoulder band, 25 tail
/// (unequip bit 1&lt;&lt;28), 26 left shoulder bag, 27 wings.
/// Prefixes: 108 face, 109 wig, 112 handheld/bags, 114 wings/backpacks/tails, 116 necklace,
/// 117 head accessories and 118 masks occupy the hat cell.
/// </summary>
internal static class AccessoryAttachMap
{
    public const int GlassesItemIdMax = 10800062;

    public static uint ToSocketBit(int itemId, uint seedOrBit)
    {
        var slot = WindowSlot(itemId, seedOrBit);
        // Tail sits in window cell 25, but the client attach/unequip bit is 1<<28
        // (GetItemBodySpot: 0x10000000 → PART_HIP_ACCESSORY). 1<<25 is a different cell.
        if (slot == CharacterEquipmentSlotIndex.Tail)
            return (uint)WardrobeSocketBit.Tail;
        if (slot == CharacterEquipmentSlotIndex.Hat)
            return (uint)WardrobeSocketBit.Head;

        var index = (byte)slot;
        if (index >= 12)
            return 1u << index;

        // Cells 10-11 overlap bra/underwear bits; keep a high unique bit.
        return slot switch
        {
            CharacterEquipmentSlotIndex.Headband => (uint)WardrobeSocketBit.Headband,
            CharacterEquipmentSlotIndex.Glasses => (uint)WardrobeSocketBit.Glasses,
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
            114 => Prefix114(itemId),
            115 => Wrist115(seed),
            116 => CharacterEquipmentSlotIndex.Necklace,
            117 or 118 => CharacterEquipmentSlotIndex.Hat,
            122 or 123 or 124 => CharacterEquipmentSlotIndex.Handheld,
            _ => SeedFallback(seed),
        };
    }

    private static CharacterEquipmentSlotIndex Prefix114(int itemId)
    {
        if (Is114Wing(itemId))
            return CharacterEquipmentSlotIndex.Wings;
        if (Is114Tail(itemId))
            return CharacterEquipmentSlotIndex.Tail;
        return CharacterEquipmentSlotIndex.LeftShoulderBag;
    }

    private static bool Is114Wing(int itemId) =>
        itemId is >= 11400000 and <= 11400005
        || itemId is >= 11400010 and <= 11400013
        || itemId == 11400150;

    private static bool Is114Tail(int itemId) =>
        itemId is 11400070 or 11400074 or 11400080 or 11400090 or 11400100 or 11400110;

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
            25 => CharacterEquipmentSlotIndex.Tail,
            26 => CharacterEquipmentSlotIndex.LeftShoulderBag,
            27 => CharacterEquipmentSlotIndex.Wings,
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
