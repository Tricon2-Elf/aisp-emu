namespace AISpace.Common.Game;

public class ItemData
{
    public uint Key { get; set; } = 0;
    public uint SortedListPriority { get; set; } = 0;
    public uint ItemId { get; set; } = 0;
    public uint IconId { get; set; } = 0; // В оригинале это было SkillId
    public string Name { get; set; } = "N/A";
    public uint Category { get; set; } = 0;
    public uint Socket1 { get; set; } = 0;
    public uint Socket2 { get; set; } = 0;
    public string Description { get; set; } = "N/A";
    public string LimitDesc { get; set; } = "N/A";
    public uint Flags { get; set; } = 0;
    public ushort _0x0448 { get; set; } = 0;
    public uint _0x044c { get; set; } = 0;
    public uint _0x0450 { get; set; } = 0;
    public uint EmotionId { get; set; } = 0;
    public uint _0x0458 { get; set; } = 0;

    public byte[] ToBytes()
    {
        var writer = new Network.PacketWriter();
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
        return writer.ToBytes(); // 1111 байт
    }
}