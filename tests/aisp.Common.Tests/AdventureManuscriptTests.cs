using System.Text;
using aisp.Common.Game;

namespace aisp.Common.Tests;

public sealed class AdventureManuscriptTests
{
    private static byte[] Utf8(string s) => Encoding.UTF8.GetBytes(s);

    [Fact]
    public void Check_CountsSheets_AndRequiresChangeMapOnTheFirstSheet()
    {
        var good = Utf8(
            "#sheetcolor,0xfffff0c8\r\nPAGEHEADER,name\\GlowLane,\r\nCHANGEMAP,map\\ダ・カーポ島\\風見学園,timezone\\eve,\r\nWAIT,400\\msec,\r\nPAGEFOOTER\r\n#sheetcolor,0xffffffff\r\nPAGEHEADER,name\\Two\r\n  IF,flag\\中央,==,on_off\\on,\r\nPAGEFOOTER\r\n"
        );
        var datalist = Utf8("[ACTOR]\r\n0,Tansy,doll,2022031,0,0,\r\n");
        var check = AdventureManuscript.Check(good, datalist);
        Assert.True(check.Ok, check.Error);
        Assert.Equal(2, check.Pages);

        Assert.False(AdventureManuscript.Check(Utf8("CHANGEMAP,map\\x,\r\n"), datalist).Ok);
        Assert.False(
            AdventureManuscript
                .Check(
                    Utf8("PAGEHEADER,name\\A,\r\nWAIT,1\\sec,\r\nCHANGEMAP,map\\x,\r\n"),
                    datalist
                )
                .Ok
        );
        Assert.False(AdventureManuscript.Check(Utf8("PAGEHEADER,name\\A,\r\n"), datalist).Ok);
        Assert.False(AdventureManuscript.Check([0xFF, 0xFE, 0x00], datalist).Ok);
        Assert.False(AdventureManuscript.Check(good, Utf8("0,Tansy,doll\r\n")).Ok);
        // An empty datalist is what a work without actors uploads.
        Assert.True(AdventureManuscript.Check(good, []).Ok);
    }
}
