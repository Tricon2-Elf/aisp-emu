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
            "streamlink:https://twitch.tv/someone",
            assignments.Resolve("room-tv", "stream:https://x/y.m3u8", 10990100)
        );
        Assert.Equal("blank", assignments.Resolve("room-tv", "https://example.test/", null));
        Assert.Equal("blank", assignments.Resolve("room-tv", "electron:https://x", null));
        Assert.Equal("blank", assignments.Resolve("room-tv", "streamlink:https://x/y", null));
        // Ids with characters the sites do not use never reach a command line.
        Assert.Equal("blank", assignments.Resolve("room-tv", "tw:some\"one", null));
        // Anything else typed falls back to the map, then to blank.
        Assert.Equal(
            "streamlink:https://twitch.tv/someone",
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

        assignments.Set(10990100, "tw:someone");
        // The page gets the hook's form; the stored assignment keeps the friendly one.
        Assert.Equal(
            "streamlink:https://twitch.tv/someone",
            assignments.Resolve("channel-screen", null, 10990100)
        );
        Assert.Equal(
            "streamlink:https://twitch.tv/someone",
            assignments.Resolve("live-watch", null, 10990100)
        );
        Assert.Equal("twitch:someone", assignments.Get(10990100));
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
            "electron:https://player.twitch.tv/?channel=yueri&parent=aisp.moe",
            ScreenAssignments.ToHookSource("twe:yueri")
        );
        Assert.Equal(
            "streamlink:https://live.nicovideo.jp/watch/lv351315472",
            ScreenAssignments.ToHookSource("lv351315472")
        );
        Assert.Equal(
            "streamlink:https://www.youtube.com/watch?v=abc",
            ScreenAssignments.ToHookSource("ytl:abc")
        );
        Assert.Equal("twitch:yueri", ScreenAssignments.Normalize(" tw:yueri "));
    }
}
