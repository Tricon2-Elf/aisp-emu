using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Network.Tests;

public class UccVoiceRegistryPacketTests
{
    [Fact]
    public void Voice_definition_matches_the_client_parser()
    {
        var payload = new UccVoiceBaseListResponse([
            new(10001, 14300001, "少女ａ", false, "タイトルコール１"),
        ]).ToBytes();
        Assert.Equal(8 + 874, payload.Length);
        var reader = new PacketReader(payload);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(10001u, reader.ReadUInt());
        Assert.Equal(14300001u, reader.ReadUInt());
        Assert.Equal("少女ａ", reader.ReadFixedString(96));
        Assert.Equal(0, reader.ReadByte());
        Assert.Equal("タイトルコール１", reader.ReadFixedString(765));
        Assert.Equal(0u, reader.ReadUInt());
    }
}
