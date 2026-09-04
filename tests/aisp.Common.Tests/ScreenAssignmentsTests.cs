using aisp.Common.Game;

namespace aisp.Common.Tests;

public sealed class ScreenAssignmentsTests
{
    [Fact]
    public void RoomTv_PlaysTheTypedIds_OtherwiseTheMapAssignment()
    {
        var assignments = new ScreenAssignments();
        assignments.Set(10990100, " twitch:someone ");

        // A Twitch channel typed into the TV wins, whatever the map says; tw: is the short form.
        Assert.Equal(
            "streamlink:https://twitch.tv/me",
            assignments.Resolve("room-tv", "twitch:me", 10990100)
        );
        Assert.Equal(
            "streamlink:https://twitch.tv/averylongchannelname",
            assignments.Resolve("room-tv", " tw:averylongchannelname ", null)
        );
        // twe: is the Twitch player embed in the off-screen browser.
        Assert.Equal(
            "electron:https://player.twitch.tv/?channel=ironmouse&parent=aisp.moe",
            assignments.Resolve("room-tv", "twe:ironmouse", null)
        );
        // YouTube: ytl: is a live stream through streamlink.
        Assert.Equal(
            "streamlink:https://www.youtube.com/watch?v=jfKfPfyJRdk",
            assignments.Resolve("room-tv", "ytl:jfKfPfyJRdk", null)
        );
        // Nico: a bare lv… id is a live programme through streamlink.
        Assert.Equal(
            "streamlink:https://live.nicovideo.jp/watch/lv351315472",
            assignments.Resolve("room-tv", "lv351315472", 10990100)
        );
        Assert.Equal(
            "streamlink:https://live.nicovideo.jp/watch/lv1",
            assignments.Resolve("room-tv", "nico:lv1", null)
        );
        // The hook's test pattern; bare pattern is the same thing.
        Assert.Equal("pattern:live", assignments.Resolve("room-tv", "pattern:live", null));
        Assert.Equal("pattern:live", assignments.Resolve("room-tv", "pattern", null));
        // The title card and the calibration grid can be typed too; the c: rectangles cannot.
        Assert.Equal("title", assignments.Resolve("room-tv", "title", 10990100));
        Assert.Equal("calibrate", assignments.Resolve("room-tv", "Calibrate", 10990100));
        Assert.Equal("blank", assignments.Resolve("room-tv", "c:0/0:10/10", null));
        // Typed URLs and streams are not honoured: they would make every viewer fetch them.
        Assert.Equal(
            "streamlink:https://twitch.tv/someone rolloff:-17340/375/-20639/1000/12000/1/0",
            assignments.Resolve("room-tv", "stream:https://x/y.m3u8", 10990100)
        );
        Assert.Equal("blank", assignments.Resolve("room-tv", "https://example.test/", null));
        Assert.Equal("blank", assignments.Resolve("room-tv", "electron:https://x", null));
        Assert.Equal("blank", assignments.Resolve("room-tv", "streamlink:https://x/y", null));
        // Ids with characters the sites do not use never reach a command line.
        Assert.Equal("blank", assignments.Resolve("room-tv", "tw:some\"one", null));
        // Anything else typed falls back to the map, then to blank.
        Assert.Equal(
            "streamlink:https://twitch.tv/someone rolloff:-17340/375/-20639/1000/12000/1/0",
            assignments.Resolve("room-tv", "Hello World", 10990100)
        );
        Assert.Equal("blank", assignments.Resolve("room-tv", "Hello World", 19001003));
        Assert.Equal("blank", assignments.Resolve("room-tv", "Hello World", null));
        // testscreen always shows the diagnostic page, even on an assigned map.
        Assert.Null(assignments.Resolve("room-tv", "testscreen", 10990100));
        assignments.Set(19001003, "TestScreen");
        Assert.Null(assignments.Resolve("live-watch", null, 19001003));
    }

    [Fact]
    public void TownScreens_FollowTheMapAssignment_UntilCleared()
    {
        var assignments = new ScreenAssignments();
        // Unassigned town screens show the title card.
        Assert.Equal("title", assignments.Resolve("channel-screen", null, 10990100));

        assignments.Set(10990100, "tw:someone https://x/banner");
        // Town screens also get the map's screen position for rolloff, unless the source names one.
        Assert.EndsWith(
            " rolloff:-17340/375/-20639/1000/12000/1/0",
            assignments.Resolve("channel-screen", null, 10990100)
        );
        // The short form keeps the map's position and only sets the range; rolloff:flat turns
        // the rolloff off; a short form on a map without a known screen is dropped.
        assignments.Set(10990100, "tw:someone rolloff:500/6000");
        Assert.Equal(
            "streamlink:https://twitch.tv/someone rolloff:-17340/375/-20639/500/6000/1/0",
            assignments.Resolve("channel-screen", null, 10990100)
        );
        // Four numbers add the gains to fade between, keeping the map's position.
        assignments.Set(10990100, "tw:someone rolloff:500/6000/0.8/0.25");
        Assert.Equal(
            "streamlink:https://twitch.tv/someone rolloff:-17340/375/-20639/500/6000/0.8/0.25",
            assignments.Resolve("channel-screen", null, 10990100)
        );
        assignments.Set(10990100, "tw:someone rolloff:flat");
        Assert.Equal(
            "streamlink:https://twitch.tv/someone",
            assignments.Resolve("channel-screen", null, 10990100)
        );
        assignments.Set(30000001, "tw:someone rolloff:500/6000");
        Assert.Equal(
            "streamlink:https://twitch.tv/someone",
            assignments.Resolve("channel-screen", null, 30000001)
        );
        assignments.Set(10990100, "tw:someone rolloff:1/2/3/10/20");
        Assert.Equal(
            "streamlink:https://twitch.tv/someone rolloff:1/2/3/10/20/1/0",
            assignments.Resolve("channel-screen", null, 10990100)
        );
        // Seven is the hook's own form and passes through unchanged.
        assignments.Set(10990100, "tw:someone rolloff:1/2/3/10/20/0.5/0.1");
        Assert.Equal(
            "streamlink:https://twitch.tv/someone rolloff:1/2/3/10/20/0.5/0.1",
            assignments.Resolve("channel-screen", null, 10990100)
        );
        assignments.Set(10990100, "tw:someone https://x/banner");
        // The page gets the hook's form; the stored assignment keeps the friendly one.
        Assert.Equal(
            "streamlink:https://twitch.tv/someone https://x/banner rolloff:-17340/375/-20639/1000/12000/1/0",
            assignments.Resolve("channel-screen", null, 10990100)
        );
        Assert.Equal(
            "streamlink:https://twitch.tv/someone https://x/banner rolloff:-17340/375/-20639/1000/12000/1/0",
            assignments.Resolve("live-watch", null, 10990100)
        );
        Assert.Equal("twitch:someone https://x/banner", assignments.Get(10990100));
        // The typed ids keep their friendly form in the assignment too.
        assignments.Set(10990100, "pattern");
        Assert.Equal("pattern:live", assignments.Get(10990100));

        Assert.True(assignments.Clear(10990100));
        Assert.False(assignments.Clear(10990100));
        Assert.Equal("title", assignments.Resolve("live-watch", null, 10990100));
    }

    [Fact]
    public void Sources_AreStreamsOrPages()
    {
        // The typed ids.
        Assert.True(ScreenAssignments.IsTwitchSource("twitch:yueri"));
        Assert.True(ScreenAssignments.IsTwitchSource("tw:yueri"));
        Assert.False(ScreenAssignments.IsTwitchSource("tw:"));
        Assert.False(ScreenAssignments.IsTwitchSource("tw:yue-ri"));
        Assert.True(ScreenAssignments.IsTwitchEmbedSource("twe:ironmouse"));
        Assert.True(ScreenAssignments.IsBrowserSource("twe:ironmouse"));
        Assert.False(ScreenAssignments.IsTwitchSource("twe:ironmouse"));
        Assert.True(ScreenAssignments.IsYouTubeLiveSource("ytl:jfKfPfyJRdk"));
        Assert.False(ScreenAssignments.IsYouTubeLiveSource("ytl:"));
        Assert.False(ScreenAssignments.IsYouTubeLiveSource("ytl:a/b"));
        Assert.True(ScreenAssignments.IsNicoLiveSource("lv351315472"));
        Assert.False(ScreenAssignments.IsNicoLiveId("lvx"));
        Assert.False(ScreenAssignments.IsNicoLiveId("lv"));
        Assert.True(ScreenAssignments.IsPatternSource("pattern:live"));
        Assert.True(ScreenAssignments.IsPatternSource("pattern"));
        Assert.False(ScreenAssignments.IsPatternSource("pattern:x"));
        foreach (var typed in new[] { "tw:yueri", "twe:yueri", "ytl:abc", "lv1", "pattern:live" })
        {
            Assert.True(ScreenAssignments.IsTypedSource(typed), typed);
            Assert.True(ScreenAssignments.IsStreamSource(typed), typed);
            Assert.True(ScreenAssignments.IsValidSource(typed), typed);
        }
        // /screen only: URLs for streamlink, ffmpeg, the off-screen browser, and pages.
        foreach (
            var moderated in new[]
            {
                "streamlink:https://www.youtube.com/watch?v=x",
                "stream:http://host/a.mp4",
                "electron:https://example.com",
            }
        )
        {
            Assert.False(ScreenAssignments.IsTypedSource(moderated), moderated);
            Assert.True(ScreenAssignments.IsStreamSource(moderated), moderated);
            Assert.True(ScreenAssignments.IsValidSource(moderated), moderated);
        }
        Assert.False(ScreenAssignments.IsStreamSource("HTTP://host/page"));
        Assert.True(ScreenAssignments.IsPageUrl("HTTP://host/page"));
        Assert.True(ScreenAssignments.IsValidSource("HTTP://host/page"));
        Assert.False(ScreenAssignments.IsTypedSource("HTTP://host/page"));
        Assert.False(ScreenAssignments.IsPageUrl("twitch:yueri"));
        // Not sources: the hook's own raw forms with nothing after the prefix, other schemes.
        Assert.False(ScreenAssignments.IsValidSource("streamlink:"));
        Assert.False(ScreenAssignments.IsValidSource("stream:"));
        Assert.False(ScreenAssignments.IsValidSource("electron:"));
        Assert.False(ScreenAssignments.IsValidSource("electron:ftp://x"));
        Assert.False(ScreenAssignments.IsElectronSource("https://example.com"));
        Assert.True(ScreenAssignments.IsValidSource("testscreen"));
        Assert.True(ScreenAssignments.IsValidSource("pattern:live box:0/0/100/50"));
        Assert.True(ScreenAssignments.IsValidSource("calibrate"));
        Assert.True(ScreenAssignments.IsValidSource("title"));
        Assert.True(ScreenAssignments.IsValidSource("blank"));
        Assert.False(ScreenAssignments.IsValidSource("Hello World"));
        Assert.False(ScreenAssignments.IsValidSource("tw:yueri twitch:other"));
        Assert.False(ScreenAssignments.IsValidSource(null));
        // The hook's forms.
        Assert.Equal(
            "streamlink:https://x/y",
            ScreenAssignments.ToHookSource("streamlink:https://x/y")
        );
        Assert.Equal("pattern:live", ScreenAssignments.ToHookSource("Pattern"));
        Assert.Equal("title", ScreenAssignments.ToHookSource("title"));
        Assert.Equal(
            "electron:https://player.twitch.tv/?channel=yueri&parent=aisp.moe scroll:0/40",
            ScreenAssignments.ToHookSource("twe:yueri scroll:0/40")
        );
        Assert.Equal(
            "streamlink:https://live.nicovideo.jp/watch/lv351315472",
            ScreenAssignments.ToHookSource("lv351315472")
        );
        Assert.Equal(
            "streamlink:https://www.youtube.com/watch?v=abc",
            ScreenAssignments.ToHookSource("ytl:abc")
        );
        // A bare second URL is the raw form (a banner page, or with a box a whole-crop frame
        // page); main:<url> and banner:<url> name the panel. All three have to be pages.
        Assert.True(ScreenAssignments.IsValidSource("tw:yueri https://example.test/banner"));
        Assert.True(ScreenAssignments.IsValidSource("blank https://example.test/banner"));
        Assert.True(ScreenAssignments.IsValidSource("tw:yueri banner:https://example.test/banner"));
        Assert.True(
            ScreenAssignments.IsValidSource(
                "twe:ironmouse box:40/30/406/240 key main:https://example.test/frame.html banner:https://example.test/top"
            )
        );
        Assert.True(ScreenAssignments.IsMainWord("main:http://x/"));
        Assert.False(ScreenAssignments.IsMainWord("main:x"));
        Assert.False(ScreenAssignments.IsValidSource("tw:yueri main:frame.html"));
        Assert.False(ScreenAssignments.IsValidSource("tw:yueri banner:ftp://x"));
        Assert.Equal(
            "streamlink:https://twitch.tv/yueri box:0/0/10/10 main:https://x/f",
            ScreenAssignments.ToHookSource("tw:yueri box:0/0/10/10 main:https://x/f")
        );
        // A box:x/y/w/h word places the video inside the crop; the page can put HTML around it.
        // Slashes because the client splits chat arguments on commas.
        Assert.True(ScreenAssignments.IsValidSource("tw:yueri box:20/20/446/303"));
        Assert.True(ScreenAssignments.IsValidSource("tw:yueri https://x/ box:0/76/635/441"));
        Assert.False(ScreenAssignments.IsValidSource("tw:yueri box:20/20"));
        Assert.False(ScreenAssignments.IsValidSource("tw:yueri box:20,20,446,303"));
        // key / key:RRGGBB colour-keys the video into the page's own pixels.
        Assert.True(
            ScreenAssignments.IsValidSource("tw:yueri box:20/20/446/303 key https://x/frame")
        );
        Assert.True(ScreenAssignments.IsValidSource("tw:yueri key:100010"));
        Assert.False(ScreenAssignments.IsValidSource("tw:yueri key:12"));
        Assert.False(ScreenAssignments.IsValidSource("tw:yueri key:zzzzzz"));
        // crop:sw/sh:cx/cy renders at sw x sh and shows the box-sized window at cx,cy of it.
        Assert.True(ScreenAssignments.IsValidSource("tw:yueri crop:972/686:243/171"));
        Assert.True(ScreenAssignments.IsValidSource("tw:yueri box:20/20/446/303 crop:892/606:0/0"));
        Assert.False(ScreenAssignments.IsValidSource("tw:yueri crop:972/686"));
        Assert.False(ScreenAssignments.IsValidSource("tw:yueri crop:0/686:0/0"));
        Assert.False(ScreenAssignments.IsValidSource("tw:yueri crop:972/686:-1/0"));
        Assert.False(ScreenAssignments.IsValidSource("tw:yueri crop:972,686:0,0"));
        // fps:N picks ffmpeg's constant output rate, from the set that maps to whole samples.
        Assert.True(ScreenAssignments.IsValidSource("tw:yueri fps:60"));
        Assert.False(ScreenAssignments.IsValidSource("tw:yueri fps:24"));
        Assert.True(ScreenAssignments.IsValidSource("tw:yueri pan"));
        var panned = new ScreenAssignments();
        panned.Set(19001003, "tw:yueri pan");
        Assert.Equal(
            "streamlink:https://twitch.tv/yueri pan rolloff:0/352.1/1567/1000/12000/1/0",
            panned.Resolve("channel-screen", null, 19001003)
        );
        Assert.True(
            ScreenAssignments.IsValidSource("tw:yueri rolloff:-17340/375/-20639/1000/12000")
        );
        Assert.True(
            ScreenAssignments.IsValidSource("tw:yueri rolloff:-17340/375/-20639/1000/12000/1/0.2")
        );
        Assert.False(ScreenAssignments.IsValidSource("tw:yueri rolloff:1/2/3"));
        Assert.False(ScreenAssignments.IsValidSource("tw:yueri rolloff:1/2/3/4/5/6"));
        Assert.True(ScreenAssignments.IsValidSource("tw:yueri rolloff:500/6000"));
        Assert.True(ScreenAssignments.IsValidSource("tw:yueri rolloff:500/6000/1/0.3"));
        Assert.True(ScreenAssignments.IsValidSource("tw:yueri rolloff:flat"));
        Assert.False(ScreenAssignments.IsValidSource("tw:yueri audio:500/6000"));
        Assert.Null(ScreenAssignments.DefaultRolloffWord(30000001));
        Assert.Equal(
            "streamlink:https://twitch.tv/yueri box:20/20/446/303",
            ScreenAssignments.ToHookSource("tw:yueri box:20/20/446/303")
        );
        Assert.Equal(
            "twitch:yueri https://x/",
            ScreenAssignments.Normalize(" tw:yueri  https://x/ ")
        );
        // Browser extras: scroll pans the document, scale is zoom.
        Assert.True(ScreenAssignments.IsValidSource("electron:https://example.com scrollx:120"));
        Assert.True(ScreenAssignments.IsValidSource("electron:https://example.com scrolly:40"));
        Assert.True(ScreenAssignments.IsValidSource("electron:https://example.com scroll:120/40"));
        Assert.True(
            ScreenAssignments.IsValidSource(
                "electron:https://www.nicovideo.jp crop:800/600:50/0 scrollx:100 scale:0.75"
            )
        );
        Assert.True(ScreenAssignments.IsValidSource("twe:yueri crop:800/600:0/0 scale:0.75"));
        Assert.True(ScreenAssignments.IsScaleWord("scale:0.75"));
        Assert.True(ScreenAssignments.IsValidSource("electron:https://x scale:1"));
        Assert.False(ScreenAssignments.IsValidSource("electron:https://x scale:0"));
        Assert.False(ScreenAssignments.IsValidSource("electron:https://x scale:9"));
        Assert.Equal(
            "electron:https://example.com scroll:120/40",
            ScreenAssignments.ToHookSource("electron:https://example.com scroll:120/40")
        );
        // Town screens get the same default distance rolloff as other streams.
        var electronTown = new ScreenAssignments();
        electronTown.Set(10990100, "electron:https://example.com scrollx:16");
        Assert.Equal(
            "electron:https://example.com scrollx:16 "
                + ScreenAssignments.DefaultRolloffWord(10990100),
            electronTown.Resolve("channel-screen", null, 10990100)
        );
    }
}
