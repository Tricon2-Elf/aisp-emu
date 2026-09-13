using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Network.Tests;

public class UccAdvFigurePacketTests
{
    [Fact]
    public void Figure_record_is_377_bytes_on_the_wire()
    {
        var writer = new PacketWriter();
        new UccAdvFigure
        {
            FigureId = 14100000,
            BoxId = 1,
            Name = "長身+りりしいモデル",
            PackageId = 0,
            ModelId = 1001021,
        }.Write(writer);

        Assert.Equal(UccAdvFigure.WireSize, writer.ToBytes().Length);
    }

    [Fact]
    public void Figure_record_fields_land_where_the_client_reads_them()
    {
        var writer = new PacketWriter();
        new UccAdvFigure
        {
            FigureId = 1000,
            IconId = 14100005,
            BoxId = 1000,
            Name = "朝倉音姫モデル",
            Gender = 1,
            People = 1,
            Face = 2,
            Hairstyle = 3,
            PackageId = 5000,
            ModelId = 2012011,
        }.Write(writer);
        var bytes = writer.ToBytes();
        var reader = new PacketReader(bytes);
        Assert.Equal(1000u, reader.ReadUInt());
        Assert.Equal(14100005u, reader.ReadUInt());
        reader.ReadFixedString(UccAdvFigure.NameBytes);
        Assert.Equal(1, reader.ReadByte());
        Assert.Equal(1u, reader.ReadUInt()); // +0x6C gender
        Assert.Equal(1u, reader.ReadUInt()); // +0x70 people
        Assert.Equal(2u, reader.ReadUInt()); // +0x74 face
        Assert.Equal(3u, reader.ReadUInt()); // +0x78 hairstyle
        Assert.Equal(2012011u, reader.ReadUInt()); // +0x7C the 3D doll's model
        Assert.Equal(1000u, reader.ReadUInt()); // +0x80 box
        Assert.Equal(5000u, reader.ReadUInt()); // +0x84 package index
        // No equipment given: thirty empty slots, then the reserved words.
        for (var i = 0; i < UccAdvFigure.EquipSlotCount * 2 + 1; i++)
            Assert.Equal(0u, reader.ReadUInt());
    }

    [Fact]
    public void Base_list_is_result_count_and_records()
    {
        var figure = new UccAdvFigure
        {
            FigureId = 1000,
            BoxId = 1000,
            Name = "朝倉音姫モデル",
            ModelId = 2012011,
        };
        var payload = new UccAdvFigureBaseListResponse(0, [figure]).ToBytes();
        Assert.Equal(8 + UccAdvFigure.WireSize, payload.Length);

        var reader = new PacketReader(payload);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(1000u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt()); // no shop icon for the included licensed figure
        Assert.Equal("朝倉音姫モデル", reader.ReadFixedString(UccAdvFigure.NameBytes));
        Assert.Equal(1, reader.ReadByte());
    }

    [Fact]
    public void Commons_title_record_is_113_bytes_and_the_list_counts_them()
    {
        var writer = new PacketWriter();
        new NiconiCommonsEntry { Id = 1, Name = "ai sp@ce MEN" }.Write(writer);
        Assert.Equal(NiconiCommonsEntry.WireSize, writer.ToBytes().Length);

        var payload = new NiconiCommonsBaseListResponse(
            0,
            [
                new NiconiCommonsEntry { Id = 1, Name = "ai sp@ce MEN" },
                new NiconiCommonsEntry { Id = 2, Name = "ai sp@ce WOMEN" },
            ]
        ).ToBytes();
        Assert.Equal(8 + 2 * NiconiCommonsEntry.WireSize, payload.Length);
        var reader = new PacketReader(payload);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal("ai sp@ce MEN", reader.ReadFixedString(NiconiCommonsEntry.NameBytes));
        Assert.Equal(1, reader.ReadByte());
        Assert.Equal(0u, reader.ReadUInt());
    }

    [Fact]
    public void Base_list_rejects_more_than_100_entries()
    {
        var figures = Enumerable.Repeat(new UccAdvFigure { FigureId = 1 }, 101).ToArray();
        Assert.Throws<InvalidOperationException>(() =>
            new UccAdvFigureBaseListResponse(0, figures).ToBytes()
        );
    }
}
