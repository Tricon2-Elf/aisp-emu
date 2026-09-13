using System.Text;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Network.Tests;

public class NiconiCommonsShopPacketTests
{
    [Fact]
    public void Started_is_npc_id_cstring_name_and_visual_id()
    {
        const uint npc = 1342177339;
        const string name = "Niconi・Commons Shop";
        var payload = new NiconiCommonsShopStartedNotify(npc, name, 0).ToBytes();
        Assert.Equal(
            sizeof(uint) + Encoding.UTF8.GetByteCount(name) + 1 + sizeof(uint),
            payload.Length
        );
        var reader = new PacketReader(payload);
        Assert.Equal(npc, reader.ReadUInt());
        Assert.Equal(name, reader.ReadString());
        Assert.Equal(0u, reader.ReadUInt());
    }

    [Fact]
    public void Item_notify_is_three_counted_16_byte_arrays()
    {
        var figures = new[]
        {
            new NiconiCommonsShopItemRecord(0, 14100000, 5000, 0),
            new NiconiCommonsShopItemRecord(0, 14200000, 5000, 0),
        };
        var payload = new NiconiCommonsShopItemNotify(figures: figures).ToBytes();
        Assert.Equal(
            4 + 4 + figures.Length * NiconiCommonsShopItemRecord.WireSize + 4,
            payload.Length
        );

        var reader = new PacketReader(payload);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(14100000u, reader.ReadUInt());
        Assert.Equal(5000u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(14200000u, reader.ReadUInt());
        Assert.Equal(5000u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
    }

    [Fact]
    public void Buy_figure_request_is_id_plus_extra_byte()
    {
        var writer = new PacketWriter();
        writer.Write(14100000u);
        writer.Write((byte)1);
        var request = NiconiCommonsShopBuyFigureRequest.FromBytes(writer.ToBytes());
        Assert.Equal(14100000u, request.FigureId);
        Assert.Equal(1, request.Extra);
    }

    [Fact]
    public void Buy_r_is_result_and_remained()
    {
        var payload = new NiconiCommonsShopBuyResponse(0, 12345).ToBytes();
        var reader = new PacketReader(payload);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(12345ul, reader.ReadULong());
    }

    [Fact]
    public void Obtain_is_the_picker_figure_id()
    {
        var payload = new UccAdvFigureObtainNotify(0x0101).ToBytes();
        Assert.Equal(0x0101u, new PacketReader(payload).ReadUInt());
    }

    [Fact]
    public void Commons_obtain_and_voice_obtain_are_one_u32()
    {
        Assert.Equal(3u, new PacketReader(new NiconiCommonsObtainNotify(3).ToBytes()).ReadUInt());
        Assert.Equal(1u, new PacketReader(new UccVoiceObtainNotify(1).ToBytes()).ReadUInt());
    }

    [Fact]
    public void Ended_is_empty()
    {
        Assert.Empty(new NiconiCommonsShopEndedNotify().ToBytes());
    }
}
