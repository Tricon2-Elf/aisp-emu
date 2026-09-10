using aisp.Network.Data;

namespace aisp.Network.Tests;

public class MonsterDataTests
{
    [Fact]
    public void ToBytes_WritesDecoderExactFieldOrder()
    {
        var data = new MonsterData(2_000_001, new CharaData(2_000_001, 7_001_010, "Monster"))
        {
            Field616 = 1.25f,
            Field620 = 2.5f,
            Field624 = 3.75f,
            Field628 = 4,
            Field632 = 5,
            Field636 = 6,
            Scale = 1.5f,
            Field644 = 7,
            Field648 = 8,
        };

        var bytes = data.ToBytes();
        var reader = new PacketReader(bytes);

        Assert.Equal(MonsterData.WireSize, bytes.Length);
        Assert.Equal(2_000_001u, reader.ReadUInt());
        Assert.Equal(2_000_001u, reader.ReadUInt());
        reader.ReadBytes(CharaData.WireSize - sizeof(uint));
        Assert.Equal(1.25f, reader.ReadFloat());
        Assert.Equal(2.5f, reader.ReadFloat());
        Assert.Equal(3.75f, reader.ReadFloat());
        Assert.Equal(4u, reader.ReadUInt());
        Assert.Equal(5u, reader.ReadUInt());
        Assert.Equal(6u, reader.ReadUInt());
        Assert.Equal(1.5f, reader.ReadFloat());
        Assert.Equal(7u, reader.ReadUInt());
        Assert.Equal(8u, reader.ReadUInt());
    }
}
