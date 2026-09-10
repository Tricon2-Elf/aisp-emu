namespace aisp.Network.Data;

public class MonsterData(CharaData chara, uint result = 0)
{
    // 606 bytes (+ 2 bytes VCE HeaderSize = 608 bytes in code)
    public const int WireSize = 606;

    public uint Result { get; set; } = result; // 4-byte header
    public CharaData Chara { get; set; } = chara; // 566-byte CharaData

    // Unclear what this does
    public uint SpawnState { get; set; } = 1;

    public uint MonsterId { get; set; } = 1;
    public uint TeamId { get; set; } = 2; // team
    public uint AiScriptId { get; set; } = 7001010; // Unclear what this affects, but if it diverges from enterendhandler a different model appears
    public uint Experience { get; set; } = 10;
    public uint DropTableId { get; set; } = 0;

    // Unclear how it works, but it does — model scale, though it does not change any further above
    public float ScaleX { get; set; } = 1.0f;
    public float ScaleY { get; set; } = 1.0f;
    public float ScaleZ { get; set; } = 1.0f;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();

        writer.Write(Result);
        writer.Write(Chara.ToBytes());
        writer.Write(1.0f); // Field_0x238
        writer.Write(MonsterId); // 4 bytes
        writer.Write(TeamId); // 4 bytes
        writer.Write(AiScriptId); // 4 bytes
        writer.Write(Experience); // 4 bytes
        writer.Write(DropTableId); // 4 bytes
        writer.Write(ScaleX); // 4 bytes
        writer.Write(ScaleY); // 4 bytes
        writer.Write(ScaleZ); // 4 bytes

        var finalBytes = writer.ToBytes();
        if (finalBytes.Length != WireSize)
        {
            throw new InvalidOperationException(
                $"MonsterData packet is {finalBytes.Length} bytes, but MUST be {WireSize}!"
            );
        }

        return finalBytes;
    }
}
