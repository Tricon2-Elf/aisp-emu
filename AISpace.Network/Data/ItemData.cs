namespace AISpace.Network.Data;

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

    // flags (0x2 = non tradable)
    public uint Flags { get; set; } = 0;

    public ushort _0x0448 { get; set; } = 0;
    public uint _0x044c { get; set; } = 0; // used as key in some map
    public uint _0x0450 { get; set; } = 0;
    public uint EmotionId { get; set; } = 0;
    public uint _0x0458 { get; set; } = 0;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Key);
        writer.Write(SortedListPriority);
        writer.Write(ItemId);
        writer.Write(IconId);
        writer.WriteFixedString(Name, 97, "Shift_JIS");
        writer.Write(Category);
        writer.Write(Socket1);
        writer.Write(Socket2);
        writer.WriteFixedString(Description, 769, "Shift_JIS");
        writer.WriteFixedString(LimitDesc, 193, "Shift_JIS");
        writer.Write(Flags);
        writer.Write(_0x0448);
        writer.Write(_0x044c);
        writer.Write(_0x0450);
        writer.Write(EmotionId);
        writer.Write(_0x0458);

        return writer.ToBytes();
    }
}
