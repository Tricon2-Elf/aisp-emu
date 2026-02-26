namespace AISpace.Common.Game;

public class ItemData
{
    public uint Key { get; set; } = 0;
    public uint SortedListPriority { get; set; } = 0;
    public uint ItemId { get; set; } = 0;
    
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string LimitDesc { get; set; } = "";

    public uint IconId { get; set; } = 0; // SkillId
    public uint Socket1 { get; set; } = 0; // Bodyspot1
    public uint Category { get; set; } = 0;
    public uint Socket2 { get; set; } = 0; // Bodyspot2_Selectable
    public uint UnkMapIdx { get; set; } = 0; // dword_44c
    public uint Flags { get; set; } = 0;
    public uint Dword450 { get; set; } = 0;
    public ushort Word448 { get; set; } = 0;
    public uint EmotionId { get; set; } = 0; // dword_454
    public uint Dword458 { get; set; } = 0;

    public byte[] ToBytes()
    {
        var writer = new Network.PacketWriter();
        
        // 1. Header (12 bytes)
        writer.Write(Key);
        writer.Write(SortedListPriority);
        writer.Write(ItemId);

        // 2. Strings (Fixed length + Padding)
        
        // Name: 97 + 3 = 100 bytes
        writer.WriteFixedString(Name, 97, "Shift_JIS");
        writer.Write(new byte[3]);

        // Description: 769 + 3 = 772 bytes
        writer.WriteFixedString(Description, 769, "Shift_JIS");
        writer.Write(new byte[3]);

        // LimitDesc: 193 + 3 = 196 bytes
        writer.WriteFixedString(LimitDesc, 193, "Shift_JIS");
        writer.Write(new byte[3]);

        writer.Write(IconId);   // pData->skill_id
        writer.Write(Socket1);  // pData->bodyspot1
        writer.Write(Category); // pData->category_skilleq20
        writer.Write(Socket2);  // pData->bodyspot2_selectable
        
        writer.Write(UnkMapIdx); // pData->dword_44c
        writer.Write(Flags);     // pData->flags
        writer.Write(Dword450);  // pData->dword_450
        
        writer.Write(Word448);     // pData->word_448 (2 bytes)
        writer.Write(new byte[2]); // Alignment

        writer.Write(EmotionId); // pData->dword_454
        writer.Write(Dword458);  // pData->dword_458

        return writer.ToBytes();
    }
}
