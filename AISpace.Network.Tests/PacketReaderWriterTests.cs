using System.Text;
using AISpace.Network;

namespace AISpace.Network.Tests;

public class PacketReaderWriterTests
{
    [Fact]
    public void PacketReader_ReadPastEnd_Throws()
    {
        Assert.Throws<EndOfStreamException>(() =>
        {
            var reader = new PacketReader(new byte[] { 1, 2 });
            reader.ReadByte();
            reader.ReadByte();
            reader.ReadByte();
        });
    }

    [Fact]
    public void PacketReader_ReadString_NullTerminated()
    {
        var bytes = Encoding.ASCII.GetBytes("hello\0world");
        var reader = new PacketReader(bytes);
        Assert.Equal("hello", reader.ReadString());
        Assert.Equal("world", reader.ReadString());
    }

    [Fact]
    public void PacketReader_ReadString_NoNullConsumesRest()
    {
        var bytes = Encoding.ASCII.GetBytes("abc");
        var reader = new PacketReader(bytes);
        Assert.Equal("abc", reader.ReadString());
    }

    [Fact]
    public void PacketReader_ReadFixedString_TrimsAtNull()
    {
        var buf = new byte[8];
        Encoding.ASCII.GetBytes("hi").AsSpan().CopyTo(buf);
        var reader = new PacketReader(buf);
        Assert.Equal("hi", reader.ReadFixedString(8, "ASCII"));
    }

    [Fact]
    public void PacketWriter_RoundTrip_PrimitivesAndString()
    {
        var w = new PacketWriter();
        w.Write((byte)0xAB);
        w.Write((sbyte)-3);
        w.Write((ushort)0x1122);
        w.Write(0x11223344u);
        w.Write(1.25f);
        w.Write(0x8899AABBCCDDEEFFUL);
        w.Write("ok");

        var r = new PacketReader(w.ToBytes());
        Assert.Equal(0xAB, r.ReadByte());
        Assert.Equal(-3, r.ReadSByte());
        Assert.Equal(0x1122, r.ReadUShort());
        Assert.Equal(0x11223344u, r.ReadUInt());
        Assert.Equal(1.25f, r.ReadFloat());
        Assert.Equal(0x8899AABBCCDDEEFFUL, r.ReadULong());
        Assert.Equal("ok", r.ReadString());
    }

    [Fact]
    public void PacketWriter_FixedAsciiString_RoundTrip()
    {
        const int len = 8;
        var w = new PacketWriter();
        w.WriteFixedAsciiString("abc", len);
        var r = new PacketReader(w.ToBytes());
        Assert.Equal("abc", r.ReadFixedString(len, "ASCII"));
    }
}
