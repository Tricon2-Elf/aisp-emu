namespace AISpace.Network.Data;

public sealed class RoboData
{
    private const int TrailingZeroCount = 183 + 296 + 81;

    public uint RoboId { get; }
    public uint State { get; }
    public CharaData Chara { get; }

    public RoboData(uint roboId, CharaData chara, uint state = 0)
    {
        RoboId = roboId;
        Chara = chara;
        State = state;
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(RoboId);
        writer.Write(0u); // dword_4
        writer.Write(State); // dword_8
        writer.Write(0u); // dword_c
        writer.Write((ushort)0); // field_10
        writer.Write(Chara.ToBytes());
        writer.Write(new byte[TrailingZeroCount]);
        return writer.ToBytes();
    }
}
