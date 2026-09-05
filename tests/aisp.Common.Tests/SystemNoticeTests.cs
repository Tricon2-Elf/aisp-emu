using System.Text;
using aisp.Common.Game;
using Xunit;

namespace aisp.Common.Tests;

public class SystemNoticeTests
{
    [Fact]
    public void Messages_KeepEveryMessageUnderTheClientLimit()
    {
        var usage =
            "/screen <source> [extras]. Sources: twitch:<channel> (or tw:), lv<id> (Nico Live), stream:<url>, a web page URL,\n"
            + "fps:N (15/20/25/30/50/60), rolloff:near/far, rolloff:near/far/max/min (gains to fade between, default 1/0), rolloff:x/y/z/near/far, rolloff:x/y/z/near/far/max/min or rolloff:flat, pan to also stereo-pan by bearing.";
        usage = usage + "\n" + usage + "\n" + usage;
        var lines = SystemNotice.Messages(usage).ToList();
        Assert.True(lines.Count >= 2);
        Assert.All(
            lines,
            line => Assert.True(Encoding.UTF8.GetByteCount(line) <= SystemNotice.MaxLineBytes)
        );
        // Nothing lost: the words come back in order.
        Assert.Equal(
            usage.Replace("\n", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries),
            string.Join(' ', lines)
                .Replace("\n", " ")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        );
        // Short multi-line notices stay one message, with their line breaks.
        Assert.Equal(new[] { "1 alice\n2 bob" }, SystemNotice.Messages("1 alice\n2 bob").ToList());
    }

    [Fact]
    public void Messages_SplitLongWordsOnCharacterBoundaries()
    {
        var url = "https://example.test/" + new string('あ', 80);
        var lines = SystemNotice.Messages(url, 40).ToList();
        Assert.True(lines.Count > 1);
        Assert.All(lines, line => Assert.True(Encoding.UTF8.GetByteCount(line) <= 40));
        Assert.Equal(url, string.Concat(lines));
        Assert.Equal(new[] { "short" }, SystemNotice.Messages("short").ToList());
    }
}
