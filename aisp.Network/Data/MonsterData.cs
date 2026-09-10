namespace aisp.Network.Data;

public class MonsterData(uint objectId, CharaData chara)
{
    // uint object id + CharaData + float[3] + uint[3] + float + uint[2]
    public const int WireSize = 606;

    public uint ObjectId { get; set; } = objectId;
    public CharaData Chara { get; set; } = chara;

    // ReadMonsterData: vec3 at +616, then uint[3], float, uint[2].
    // InitChara127 treats the uint at +628 as m_Type; case 1 is the enemy controller.
    public float Field616 { get; set; } = 1.0f;
    public float Field620 { get; set; } = 1.0f;
    public float Field624 { get; set; } = 1.0f;
    public uint Field628 { get; set; } = 1;
    public uint Field632 { get; set; } = 1;
    public uint Field636 { get; set; } = 2;
    public float Scale { get; set; } = 1.0f;
    public uint Field644 { get; set; }
    public uint Field648 { get; set; }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();

        writer.Write(ObjectId);
        writer.Write(Chara.ToBytes());
        writer.Write(Field616);
        writer.Write(Field620);
        writer.Write(Field624);
        writer.Write(Field628);
        writer.Write(Field632);
        writer.Write(Field636);
        writer.Write(Scale);
        writer.Write(Field644);
        writer.Write(Field648);

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
