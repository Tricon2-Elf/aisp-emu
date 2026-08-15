using aisp.Network;

namespace aisp.Network.Data;

public enum EmotionCategory : byte
{
    Passion = 0,
    Action = 1,
    Voice = 2,
    Etc = 3,
}

public class EmotionData
{
    public uint Id { get; set; }
    public string Name { get; set; } = "";
    public EmotionCategory Category { get; set; } = EmotionCategory.Passion;

    public void Write(PacketWriter writer)
    {
        writer.Write(Id);
        writer.WriteFixedString(Name, 96);
        writer.Write((byte)Category);
        writer.Write((byte)0); // _0x65 — always 0
        writer.Write((byte)1); // Flag1 — always 1
        writer.Write((byte)1); // Flag2 — always 1
        writer.Write((byte)1); // Flag3 — always 1
        writer.Write((byte)1); // Flag4 — always 1
    }
}
