using AISpace.Network;

namespace AISpace.Network.Data;

public class EmotionData
{
    public uint Id { get; set; }
    public string Name { get; set; } = "";
    public byte Category { get; set; } = 0;
    public byte Unk2 { get; set; } = 0;
    public bool Flag1 { get; set; } = true;
    public bool Flag2 { get; set; } = true;
    public bool Flag3 { get; set; } = true;
    public bool Flag4 { get; set; } = true;

    public void Write(PacketWriter writer)
    {
        writer.Write(Id);
        writer.WriteFixedString(Name, 96, "Shift_JIS");
        writer.Write(Category);
        writer.Write(Unk2);
        writer.Write((byte)(Flag1 ? 1 : 0));
        writer.Write((byte)(Flag2 ? 1 : 0));
        writer.Write((byte)(Flag3 ? 1 : 0));
        writer.Write((byte)(Flag4 ? 1 : 0));
    }
}
