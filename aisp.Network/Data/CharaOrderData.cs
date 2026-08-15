namespace aisp.Network.Data;

/// <summary>
/// Wire size is 285 bytes (in-memory struct is 288 with 3 bytes padding before Params).
/// Populates the client item-limit map used by wardrobe equip checks (sub_406E60).
/// </summary>
public class CharaOrderData(uint category, byte limitByte1 = 0, byte limitByte2 = 0)
{
    /// <summary>Client m_ControllerType bit for player avatars.</summary>
    public const byte ControllerAvatar = 1;

    /// <summary>Client m_ControllerType bit for Charadolls / Robos.</summary>
    public const byte ControllerRobo = 2;

    /// <summary>Allow wardrobe equip on both avatars and Charadolls.</summary>
    public const byte ControllerAvatarOrRobo = ControllerAvatar | ControllerRobo;

    public uint Category { get; set; } = category;
    public byte LimitByte1 { get; set; } = limitByte1;
    public byte LimitByte2 { get; set; } = limitByte2;

    /// <summary>
    /// Client offline defaults (sub_48FB30) mix male/female and avatar/robo limit bytes per
    /// category, which blocks wardrobe re-equip via sub_406E60. Send gender-matched orders with
    /// LimitByte1 covering both avatar (1) and Robo/Charadoll (2) controller types.
    /// </summary>
    public static IReadOnlyList<CharaOrderData> ForGender(int gender)
    {
        // Client sub_406E60: v7 = (genderMethod() != 1) + 1 → 1 male, 2 female.
        byte limitByte2 = (byte)(gender == 1 ? 1 : 2);
        return
        [
            new(101, ControllerAvatarOrRobo, limitByte2), // shirt
            new(102, ControllerAvatarOrRobo, limitByte2), // pants / skirt
            new(103, ControllerAvatarOrRobo, limitByte2), // gloves
            new(104, ControllerAvatarOrRobo, limitByte2), // socks
            new(105, 0, limitByte2), // shoes
            new(106, 0, limitByte2), // bra
            new(107, 0, limitByte2), // lower underwear
            new(200), // accessories / misc
        ];
    }

    public static IReadOnlyList<CharaOrderData> DefaultClothingOrders { get; } = ForGender(1);

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Category);
        writer.Write(new byte[193]);
        writer.Write(LimitByte1);
        writer.Write(LimitByte2);
        writer.Write((byte)0);
        writer.Write((byte)0);
        // Params follow immediately on the wire (no padding before 0xCC in-memory offset).
        for (var i = 0; i < 20; i++)
            writer.Write(0u);
        writer.Write(0u);
        return writer.ToBytes();
    }
}
