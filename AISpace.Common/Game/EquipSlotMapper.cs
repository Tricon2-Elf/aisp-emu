namespace AISpace.Common.Game;

internal static class EquipSlotMapper
{
    /// <summary>
    /// Maps a preview equip entry to a CharaData slot index for DB persistence.
    /// Matches default male create slots: shirt=0, pants=1, socks=2, shoes=3, underwear=4.
    /// </summary>
    public static bool TryResolveSlotIndex(uint itemId, uint socketBit, out byte slotIndex)
    {
        slotIndex = 0;
        if (itemId == 0)
            return false;

        if (itemId is >= 10_000_000 and < 200_000_000)
        {
            slotIndex = (itemId / 100_000) switch
            {
                101 => 0,
                102 => 1,
                104 => 2,
                105 => 3,
                107 => 4,
                106 => 5,
                _ => byte.MaxValue,
            };
            if (slotIndex != byte.MaxValue)
                return true;
        }

        if (socketBit != 0)
        {
            slotIndex = socketBit switch
            {
                8 => 0,
                16 or 32 => 1,
                128 => 2,
                256 or 512 => 3,
                2048 => 4,
                1024 => 5,
                _ => byte.MaxValue,
            };
            if (slotIndex != byte.MaxValue)
                return true;
        }

        return false;
    }
}
