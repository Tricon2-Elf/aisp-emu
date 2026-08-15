namespace aisp.Common.Game;

internal enum CharacterEquipmentSlotIndex : byte
{
    Shirt = 0,
    LowerBody = 1,
    Socks = 2,
    Shoes = 3,
    LowerUnderwear = 4,
    Bra = 5,
    Hat = 6,
    Gloves = 7,
    Coat = 8,
    Jacket = 9,
}

internal static class EquipSlotMapper
{
    private const byte InvalidSlot = byte.MaxValue;

    public static bool TryResolveSlotIndex(uint itemId, uint socketBit, out byte slotIndex)
    {
        slotIndex = 0;
        if (itemId == 0)
            return false;

        if (itemId is >= 10_000_000 and < 200_000_000)
        {
            slotIndex = (itemId / 100_000) switch
            {
                100 => (byte)CharacterEquipmentSlotIndex.Hat,
                101 => (byte)CharacterEquipmentSlotIndex.Shirt,
                102 => (byte)CharacterEquipmentSlotIndex.LowerBody,
                103 => (byte)CharacterEquipmentSlotIndex.Gloves,
                104 => (byte)CharacterEquipmentSlotIndex.Socks,
                105 => (byte)CharacterEquipmentSlotIndex.Shoes,
                107 => (byte)CharacterEquipmentSlotIndex.LowerUnderwear,
                106 => (byte)CharacterEquipmentSlotIndex.Bra,
                _ => InvalidSlot,
            };
            if (slotIndex != InvalidSlot)
                return true;
        }

        if (socketBit != 0)
        {
            slotIndex = socketBit switch
            {
                1 => (byte)CharacterEquipmentSlotIndex.Hat, // hat/head
                2 => (byte)CharacterEquipmentSlotIndex.Coat, // upper-body layer 1 (coat)
                4 => (byte)CharacterEquipmentSlotIndex.Jacket, // upper-body layer 2 (jacket)
                8 => (byte)CharacterEquipmentSlotIndex.Shirt, // upper-body layer 3 (shirt)
                16 or 32 => (byte)CharacterEquipmentSlotIndex.LowerBody,
                64 => (byte)CharacterEquipmentSlotIndex.Gloves,
                128 => (byte)CharacterEquipmentSlotIndex.Socks,
                256 or 512 => (byte)CharacterEquipmentSlotIndex.Shoes,
                2048 => (byte)CharacterEquipmentSlotIndex.LowerUnderwear,
                1024 => (byte)CharacterEquipmentSlotIndex.Bra,
                _ => InvalidSlot,
            };
            if (slotIndex != InvalidSlot)
                return true;
        }

        return false;
    }
}
