using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Network.Tests;

public class NiconiCommonsRegistryPacketTests
{
    [Theory]
    [InlineData(2u, 32001001u)]
    [InlineData(3u, 42001001u)]
    public void Commons_definition_selects_the_correct_registry(uint type, uint id)
    {
        var payload = new NiconiCommonsBaseListResponse(
            0,
            [
                new()
                {
                    Id = id,
                    IconId = 14300002,
                    Type = type,
                    Name = "朝",
                    Available = false,
                },
            ]
        ).ToBytes();
        Assert.Equal(121, payload.Length);
        var reader = new PacketReader(payload);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(id, reader.ReadUInt());
        Assert.Equal(14300002u, reader.ReadUInt());
        Assert.Equal(type, reader.ReadUInt());
        Assert.Equal("朝", reader.ReadFixedString(96));
        Assert.Equal(0, reader.ReadByte());
        Assert.Equal(0u, reader.ReadUInt());
    }
}
