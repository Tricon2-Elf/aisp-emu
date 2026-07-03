using System;

namespace AISpace.Network.Data;

[Flags]
public enum ItemFlags : uint
{
    None = 0,

    /// Unknown attribute icon in item detail UI
    Unknown1 = 1 << 0,

    /// Non-tradable (blocks trade/gacha, equip slot 3)
    NonTradable = 1 << 1,

    /// Equippable in slot (slot types 0-2)
    Equippable = 1 << 2,

    /// Category 15 visible (visible in skill equip UI)
    Category15Visible = 1 << 3,

    /// Category 15 usable (skill is active/usable; if NOT set, UI is disabled)
    Category15Usable = 1 << 4,

    /// Permits layering another item in the underwear-top slot (socket 0x400).
    /// Without this flag, equipping over 0x400 is blocked if anything already occupies it.
    PermitsUnderwearTop = 1 << 5,

    /// Permits layering another item in the underwear-bottom slot (socket 0x800).
    /// Without this flag, equipping over 0x800 is blocked if anything already occupies it.
    PermitsUnderwearBottom = 1 << 6,

    /// Unknown attribute icon in item detail UI
    Unknown2 = 1 << 7,
}

public class ItemData
{
    // used as key in item manager
    public uint Key { get; set; } = 0;

    // used as priority in sorted list
    public uint SortedListPriority { get; set; } = 0;

    // used for item in data
    public uint ItemId { get; set; } = 0;

    /// Used in skill manager; client also uses this as icon id for inventory
    public uint IconId { get; set; } = 0;

    public string Name { get; set; } = "N/A";

    // seems like rest is item types, 20 is skill?
    // [0-11] = ?
    // [12-14] = ?
    // [15-16] = ?
    // [17] = ?
    // [20] = skill
    public uint Category { get; set; } = 0;

    public uint Socket1 { get; set; } = 0; // PartSocket
    public uint Socket2 { get; set; } = 0; // PartSocket

    public string Description { get; set; } = "N/A";
    public string LimitDesc { get; set; } = "N/A";

    public ItemFlags Flags { get; set; } = ItemFlags.None;

    // Maximum copies of this item a player can own.
    // Client logic treats 0 as a hard zero-cap in several flows.
    public ushort MaxPossessionCount { get; set; } = 0;

    // Key for furniture/placement/item-box map lookup (cls_491020::m_UnkMap)
    public uint PlacementTypeId { get; set; } = 0;

    // Unknown; passed through to client, no server-side business logic
    public uint _0x0450 { get; set; } = 0;

    public uint EmotionId { get; set; } = 0;

    // Item grade/level displayed in item detail UI (0 = no grade display)
    public uint Grade { get; set; } = 0;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Key);
        writer.Write(SortedListPriority);
        writer.Write(ItemId);
        writer.Write(IconId);
        writer.WriteFixedString(Name, 97, "utf-8");
        writer.Write(Category);
        writer.Write(Socket1);
        writer.Write(Socket2);
        writer.WriteFixedString(Description, 769, "utf-8");
        writer.WriteFixedString(LimitDesc, 193, "utf-8");
        writer.Write((uint)Flags);
        writer.Write(MaxPossessionCount);
        writer.Write(PlacementTypeId);
        writer.Write(_0x0450);
        writer.Write(EmotionId);
        writer.Write(Grade);

        return writer.ToBytes();
    }
}
