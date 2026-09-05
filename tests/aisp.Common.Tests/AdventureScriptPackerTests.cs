using System.Buffers.Binary;
using System.Text;
using aisp.Common.Game;

namespace aisp.Common.Tests;

public sealed class AdventureScriptPackerTests
{
    [Fact]
    public void Pack_MatchesTheOfficialCacheLayout_AndRoundTrips()
    {
        // The client uploads UTF-8 text; the cache holds UTF-16LE without BOM, LF or CRLF as written.
        var script = Encoding.UTF8.GetBytes(
            "#sheetcolor,0xffffffff\r\nPAGEHEADER,name\\Main_001,\r\nCHANGEMAP,map\\ダ・カーポ島\\風見学園,timezone\\day,\r\n"
        );
        var datalist = Encoding.UTF8.GetBytes("[ACTOR]\r\n0,Tansy,doll,2022031,0,0,\r\n");
        var jammer = Enumerable.Range(1, 20).Select(i => (byte)(i * 7)).ToArray();

        var packed = AdventureScriptPacker.Pack(script, datalist, jammer);

        var chardef = Encoding.Unicode.GetBytes(Encoding.UTF8.GetString(datalist));
        var drama = Encoding.Unicode.GetBytes(Encoding.UTF8.GetString(script));
        Assert.Equal("ADV0"u8.ToArray(), packed[..4]);
        Assert.Equal(1u, BinaryPrimitives.ReadUInt32LittleEndian(packed.AsSpan(4)));
        Assert.Equal(
            (uint)packed.Length,
            BinaryPrimitives.ReadUInt32LittleEndian(packed.AsSpan(8))
        );
        Assert.Equal(
            (uint)(chardef.Length + 4),
            BinaryPrimitives.ReadUInt32LittleEndian(packed.AsSpan(12))
        );
        Assert.Equal(
            (uint)(drama.Length + 4),
            BinaryPrimitives.ReadUInt32LittleEndian(packed.AsSpan(16))
        );
        Assert.Equal(20u, BinaryPrimitives.ReadUInt32LittleEndian(packed.AsSpan(20)));
        Assert.Equal(24 + chardef.Length + 4 + drama.Length + 4 + 20, packed.Length);
        Assert.Equal(jammer, packed[^20..]);
        // First payload byte: '[' (0x5B) + jammer[0] (7); the jammer index restarts at each section.
        Assert.Equal((byte)(0x5B + 7), packed[24]);
        Assert.Equal((byte)('#' + 7), packed[24 + chardef.Length + 4]);
        // The four trailer bytes decode to zero.
        Assert.Equal(jammer[chardef.Length % 20], packed[24 + chardef.Length]);

        var unpacked = AdventureScriptPacker.Unpack(packed);
        Assert.NotNull(unpacked);
        Assert.Equal(drama, unpacked.Value.Script);
        Assert.Equal(chardef, unpacked.Value.Datalist);
    }

    [Fact]
    public void ToUtf16_AcceptsBomVariants()
    {
        var plain = Encoding.Unicode.GetBytes("abc");
        Assert.Equal(plain, AdventureScriptPacker.ToUtf16(Encoding.UTF8.GetBytes("abc")));
        Assert.Equal(
            plain,
            AdventureScriptPacker.ToUtf16([0xEF, 0xBB, 0xBF, (byte)'a', (byte)'b', (byte)'c'])
        );
        Assert.Equal(plain, AdventureScriptPacker.ToUtf16([0xFF, 0xFE, .. plain]));
        Assert.Null(AdventureScriptPacker.Unpack([1, 2, 3]));
    }
}
