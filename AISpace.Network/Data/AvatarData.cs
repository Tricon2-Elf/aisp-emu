namespace AISpace.Network.Data;

public class AvatarData(uint AvatarId, CharaData chara)
{
    public List<SpawnInitEntry> SpawnEntries { get; set; } = Enumerable.Repeat(new SpawnInitEntry(), 8).ToList();
    public BodyAppearanceData BodyAppearance { get; set; } = new();

    public uint Unk_3a8 { get; set; }           // dead — stored but never consumed by client
    public uint CurrentEmotion { get; set; }    // if non-zero, CChara::SetEmotion called on spawn
    public byte SecondaryGender { get; set; }   // m_pController2::func_61 → m_Gender

    public byte[] ToBytes()
    {
        PacketWriter writer = new();
        writer.Write(AvatarId);
        writer.Write(chara.ToBytes());
        foreach (var entry in SpawnEntries)
            writer.Write(entry.ToBytes());
        writer.Write(Unk_3a8);
        writer.Write(CurrentEmotion);
        writer.Write(SecondaryGender);
        writer.Write(BodyAppearance.ToBytes());
        return writer.ToBytes(); // 928 bytes
    }
}
