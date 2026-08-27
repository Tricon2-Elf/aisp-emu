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

    /// <summary>
    /// Skip the sub_406E60 gender equality check. Equip-order is one map for the session, and
    /// Charadolls are always female, so avatar-matched LimitByte2 (1 male / 2 female) blocks
    /// doll wardrobe on male accounts.
    /// </summary>
    public const byte GenderUnrestricted = 0;

    public uint Category { get; set; } = category;
    public byte LimitByte1 { get; set; } = limitByte1;
    public byte LimitByte2 { get; set; } = limitByte2;

    /// <summary>Fixed char[193] label (ITEM_DATA.limit_description). Left empty.</summary>
    public string LimitDesc { get; set; } = string.Empty;

    /// <summary>
    /// Client offline defaults (sub_48FB30) mix male/female and avatar/robo limit bytes per
    /// category, which blocks wardrobe re-equip via sub_406E60. Send one shared table with
    /// LimitByte1 covering avatar (1) and Robo (2), and LimitByte2 unrestricted so male
    /// avatars and female dolls can both apply clothes.
    /// </summary>
    public static IReadOnlyList<CharaOrderData> WardrobeOrders { get; } =
    [
        new(100, ControllerAvatarOrRobo, GenderUnrestricted), // hats (100xxxxx)
        new(101, ControllerAvatarOrRobo, GenderUnrestricted), // shirt
        new(102, ControllerAvatarOrRobo, GenderUnrestricted), // pants / skirt
        new(103, ControllerAvatarOrRobo, GenderUnrestricted), // gloves
        new(104, ControllerAvatarOrRobo, GenderUnrestricted), // socks
        new(105, 0, GenderUnrestricted), // shoes
        new(106, 0, GenderUnrestricted), // bra
        new(107, 0, GenderUnrestricted), // lower underwear
        new(108, ControllerAvatarOrRobo, GenderUnrestricted), // accessories (108xxxxx)
        new(109, ControllerAvatarOrRobo, GenderUnrestricted), // wigs (109xxxxx)
        new(200), // misc / leftover prefixes (bags, …)
    ];

    public static IReadOnlyList<CharaOrderData> ForGender(int gender)
    {
        _ = gender;
        return WardrobeOrders;
    }

    public static IReadOnlyList<CharaOrderData> DefaultClothingOrders { get; } = WardrobeOrders;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Category);
        writer.WriteFixedString(LimitDesc, 193);
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
