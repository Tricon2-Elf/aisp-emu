using aisp.Common.Game;

namespace aisp.Common.Tests;

public sealed class ScreenAssignmentsTests
{
    private sealed class TestTime : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public void RoomTv_PlaysTheTypedIds_OtherwiseTheMapAssignment()
    {
        var time = new TestTime();
        var assignments = new ScreenAssignments(time);
        var t0 = time.Now.ToUnixTimeSeconds();
        assignments.Set(10990100, " twl:someone ");

        // A Twitch channel typed into the TV wins, whatever the map says; tw: is the short form.
        Assert.Equal(
            "streamlink:https://twitch.tv/me",
            assignments.Resolve("room-tv", "twl:me", 10990100)
        );
        Assert.Equal(
            "streamlink:https://twitch.tv/averylongchannelname",
            assignments.Resolve("room-tv", " twl:averylongchannelname ", null)
        );
        // twe: is the Twitch player embed in the off-screen browser.
        Assert.Equal(
            "electron:https://player.twitch.tv/?channel=ironmouse&parent=aisp.moe",
            assignments.Resolve("room-tv", "twe:ironmouse", null)
        );
        // YouTube: ytl: is a live stream through streamlink, yt: a video through yt-dlp.
        Assert.Equal(
            "streamlink:https://www.youtube.com/watch?v=jfKfPfyJRdk",
            assignments.Resolve("room-tv", "ytl:jfKfPfyJRdk", null)
        );
        Assert.Equal(
            $"yt-dlp:https://www.youtube.com/watch?v=dQw4w9WgXcQ start:{t0} offset:0",
            assignments.Resolve("room-tv", "ytd:dQw4w9WgXcQ", null)
        );
        // yte: is YouTube's own embed in the off-screen browser, through this server's page
        // (root-relative: the hook fetches it from wherever the screen page came from), with
        // the shared timeline in the page's URL as well as in the words.
        Assert.Equal(
            $"electron:/ai-sp/yt-embed?v=dQw4w9WgXcQ&start={t0}&offset=0 start:{t0} offset:0",
            assignments.Resolve("room-tv", "yte:dQw4w9WgXcQ", null)
        );
        // Nico: a bare lv… id is a live programme, sm… a video, both as their watch pages in the
        // off-screen browser by default, each with a run: script of this server's: the live one
        // presses the player's own fullscreen button (Nico has no live embed and the page refuses
        // framing), the video one pins the page's video element over the box and drives it, with
        // the shared timeline in the page URL's fragment.
        Assert.Equal(
            "electron:https://live.nicovideo.jp/watch/lv351315472 run:/ai-sp/run/nicolive-player.js",
            assignments.Resolve("room-tv", "lv351315472", 10990100)
        );
        Assert.Equal(
            "electron:https://live.nicovideo.jp/watch/lv1 run:/ai-sp/run/nicolive-player.js",
            assignments.Resolve("room-tv", "nico:lv1", null)
        );
        Assert.Equal(
            $"electron:https://www.nicovideo.jp/watch/sm11273499#start={t0}&offset=0 run:/ai-sp/run/nicovideo-player.js start:{t0} offset:0",
            assignments.Resolve("room-tv", "sm11273499", 10990100)
        );
        // The explicit forms pick the path regardless of the server's default.
        Assert.Equal(
            "streamlink:https://live.nicovideo.jp/watch/lv351315472",
            assignments.Resolve("room-tv", "nnl:lv351315472", null)
        );
        Assert.Equal(
            $"yt-dlp:https://www.nicovideo.jp/watch/sm11273499 start:{t0} offset:0",
            assignments.Resolve("room-tv", "nnd:sm11273499", null)
        );
        Assert.Equal(
            $"yt-dlp:https://www.nicovideo.jp/watch/lv1 start:{t0} offset:0",
            assignments.Resolve("room-tv", "nnd:lv1:vod", null)
        );
        Assert.Equal(
            $"electron:https://www.nicovideo.jp/watch/sm9#start={t0}&offset=0 run:/ai-sp/run/nicovideo-player.js start:{t0} offset:0",
            assignments.Resolve("room-tv", "nne:sm9", null)
        );
        // The hook's test pattern, live or as a video on the shared timeline; bare pattern is live.
        Assert.Equal("pattern:live", assignments.Resolve("room-tv", "pattern:live", null));
        Assert.Equal("pattern:live", assignments.Resolve("room-tv", "pattern", null));
        Assert.Equal(
            $"pattern:vod start:{t0} offset:0",
            assignments.Resolve("room-tv", "pattern:vod", 10990100)
        );
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
        Assert.Equal("blank", assignments.Resolve("room-tv", "yt-dlp:https://x/y", null));
        // Ids with characters the sites do not use never reach a command line.
        Assert.Equal("blank", assignments.Resolve("room-tv", "twl:some\"one", null));
        Assert.Equal("blank", assignments.Resolve("room-tv", "ytd:abc&x=1", null));
        Assert.Equal("blank", assignments.Resolve("room-tv", "smx", null));
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
    public void ShortForms_GoWhereTheServerIsConfigured_EmbedsByDefault()
    {
        var time = new TestTime();
        var t0 = time.Now.ToUnixTimeSeconds();
        // tw: and yt: are kept as typed (twitch: is an alias of tw:) and resolved at hook time.
        Assert.Equal("tw:someone", ScreenAssignments.Normalize("twitch:someone"));
        Assert.Equal("tw:someone", ScreenAssignments.Normalize("tw:someone"));
        var embeds = new ScreenAssignments(time);
        Assert.Equal(
            "electron:https://player.twitch.tv/?channel=someone&parent=aisp.moe",
            embeds.Resolve("room-tv", "twitch:someone", null)
        );
        Assert.Equal(
            $"electron:/ai-sp/yt-embed?v=abc123&start={t0}&offset=0 start:{t0} offset:0",
            embeds.Resolve("room-tv", "yt:abc123", null)
        );
        Assert.True(ScreenAssignments.IsBrowserSource("tw:someone"));
        Assert.True(ScreenAssignments.IsBrowserSource("yt:abc123"));
        var decoded = new ScreenAssignments(
            time,
            new ScreenSourceDefaults(TwitchEmbed: false, YouTubeEmbed: false)
        );
        Assert.Equal(
            "streamlink:https://twitch.tv/someone",
            decoded.Resolve("room-tv", "tw:someone", null)
        );
        Assert.Equal(
            $"yt-dlp:https://www.youtube.com/watch?v=abc123 start:{t0} offset:0",
            decoded.Resolve("room-tv", "yt:abc123", null)
        );
        // Nico the same way: nn: (a bare id, or nico:) follows the server, nne:/nnd:/nnl: do not.
        Assert.Equal("nn:sm9", ScreenAssignments.Normalize("nico:sm9"));
        Assert.Equal("nn:lv1", ScreenAssignments.Normalize("lv1"));
        Assert.True(ScreenAssignments.IsBrowserSource("sm9"));
        Assert.True(ScreenAssignments.IsBrowserSource("lv1"));
        Assert.True(ScreenAssignments.IsBrowserSource("nne:sm9"));
        Assert.False(ScreenAssignments.IsBrowserSource("nnd:sm9"));
        Assert.False(ScreenAssignments.IsBrowserSource("nnl:lv1"));
        var nicoDecoded = new ScreenAssignments(
            time,
            new ScreenSourceDefaults(NicoVideoEmbed: false, NicoLiveEmbed: false)
        );
        Assert.Equal(
            $"yt-dlp:https://www.nicovideo.jp/watch/sm9 start:{t0} offset:0",
            nicoDecoded.Resolve("room-tv", "sm9", null)
        );
        Assert.Equal(
            "streamlink:https://live.nicovideo.jp/watch/lv1",
            nicoDecoded.Resolve("room-tv", "lv1", null)
        );
        Assert.True(ScreenAssignments.IsVideoSource("nne:sm9"));
        Assert.True(ScreenAssignments.IsVideoSource("nnd:lv1:vod"));
        Assert.False(ScreenAssignments.IsVideoSource("nne:lv1"));
        Assert.True(ScreenAssignments.IsTypedSource("nne:lv1"));
        Assert.True(ScreenAssignments.IsTypedSource("nnl:lv1"));
        var decodedDefaults = new ScreenSourceDefaults(TwitchEmbed: false, YouTubeEmbed: false);
        Assert.False(ScreenAssignments.IsBrowserSource("tw:someone", decodedDefaults));
        Assert.False(ScreenAssignments.IsBrowserSource("yt:abc123", decodedDefaults));
        // The explicit forms never move.
        Assert.Equal(
            "electron:https://player.twitch.tv/?channel=someone&parent=aisp.moe",
            decoded.Resolve("room-tv", "twe:someone", null)
        );
        Assert.Equal(
            "streamlink:https://twitch.tv/someone",
            embeds.Resolve("room-tv", "twl:someone", null)
        );
        // From the server's settings.
        Assert.Equal(
            new ScreenSourceDefaults(false, false),
            ScreenSourceDefaults.FromOptions(
                new aisp.Common.Config.ScreenOptions { Twitch = "streamlink", YouTube = "yt-dlp" }
            )
        );
        Assert.Equal(
            ScreenSourceDefaults.Default,
            ScreenSourceDefaults.FromOptions(new aisp.Common.Config.ScreenOptions())
        );
    }

    [Fact]
    public void YouTubeEmbed_CarriesTheTimelineInItsPageUrl()
    {
        var time = new TestTime();
        var assignments = new ScreenAssignments(time);
        var t0 = time.Now.ToUnixTimeSeconds();
        assignments.Set(30000002, "yte:abc123 box:10/20/300/200");
        // The embed page gets the timeline in its query (the off-screen browser only sees the
        // URL), the words follow for the page's title; the extras stay.
        Assert.Equal(
            $"electron:/ai-sp/yt-embed?v=abc123&start={t0}&offset=0 box:10/20/300/200 start:{t0} offset:0",
            assignments.Resolve("channel-screen", null, 30000002)
        );
        time.Now = time.Now.AddSeconds(10);
        Assert.True(assignments.Control(30000002, "pause"));
        time.Now = time.Now.AddSeconds(5);
        Assert.StartsWith(
            $"electron:/ai-sp/yt-embed?v=abc123&start={t0}&offset=0&paused={t0 + 10} ",
            assignments.Resolve("channel-screen", null, 30000002)
        );
        // Seeking moves start and offset (still paused: the pause time moves too), so the URL
        // changes and the browser restarts there.
        Assert.True(assignments.Control(30000002, "seek:100"));
        Assert.StartsWith(
            $"electron:/ai-sp/yt-embed?v=abc123&start={t0 + 15}&offset=100&paused={t0 + 15} ",
            assignments.Resolve("channel-screen", null, 30000002)
        );
        Assert.True(assignments.Control(30000002, "resume"));
        Assert.StartsWith(
            $"electron:/ai-sp/yt-embed?v=abc123&start={t0 + 15}&offset=100 ",
            assignments.Resolve("channel-screen", null, 30000002)
        );
    }

    [Fact]
    public void Videos_CarryASharedTimeline_ThatPauseResumeAndSeekMove()
    {
        var time = new TestTime();
        var assignments = new ScreenAssignments(time);
        var t0 = time.Now.ToUnixTimeSeconds();
        assignments.Set(30000001, "ytd:abc123");
        Assert.Equal(
            $"yt-dlp:https://www.youtube.com/watch?v=abc123 start:{t0} offset:0",
            assignments.Resolve("channel-screen", null, 30000001)
        );
        // Ten seconds in, the words do not change: the position follows from start alone.
        time.Now = time.Now.AddSeconds(10);
        Assert.EndsWith(
            $"start:{t0} offset:0",
            assignments.Resolve("channel-screen", null, 30000001)
        );
        Assert.Equal(10, assignments.GetTimeline(30000001)!.PositionAt(time.Now));
        // Pause: the position freezes at 10, the words gain the pause time.
        Assert.True(assignments.Control(30000001, "pause"));
        time.Now = time.Now.AddSeconds(5);
        Assert.Equal(10, assignments.GetTimeline(30000001)!.PositionAt(time.Now));
        Assert.EndsWith(
            $"start:{t0} offset:0 paused:{t0 + 10}",
            assignments.Resolve("channel-screen", null, 30000001)
        );
        // Resume: a new start at now with the paused position as offset.
        Assert.True(assignments.Control(30000001, "resume"));
        Assert.EndsWith(
            $"start:{t0 + 15} offset:10",
            assignments.Resolve("channel-screen", null, 30000001)
        );
        time.Now = time.Now.AddSeconds(2);
        Assert.Equal(12, assignments.GetTimeline(30000001)!.PositionAt(time.Now));
        // Seek while playing.
        Assert.True(assignments.Control(30000001, "seek:100"));
        Assert.EndsWith(
            $"start:{t0 + 17} offset:100",
            assignments.Resolve("channel-screen", null, 30000001)
        );
        // Not a video: nothing to control.
        assignments.Set(30000001, "twl:someone");
        Assert.False(assignments.Control(30000001, "pause"));
        Assert.False(assignments.Control(30000002, "pause"));
        // The other videos: a Nico video and the vod pattern.
        assignments.Set(30000001, "sm9");
        Assert.True(assignments.Control(30000001, "pause"));
        assignments.Set(30000001, "pattern:vod");
        Assert.True(assignments.Control(30000001, "seek:30"));
        Assert.Equal(
            $"pattern:vod start:{t0 + 17} offset:30",
            assignments.Resolve("channel-screen", null, 30000001)
        );
        // A video typed into a room TV gets a timeline shared by that TV, in that room: map plus
        // channel, since room instances reuse the same map id.
        Assert.Equal(
            $"yt-dlp:https://www.youtube.com/watch?v=xyz start:{t0 + 17} offset:0",
            assignments.Resolve("room-tv", "ytd:xyz", 10990100, 1)
        );
        Assert.True(assignments.ControlMovie(10990100, 1, "ytd:xyz", "pause"));
        Assert.EndsWith(
            $"paused:{t0 + 17}",
            assignments.Resolve("room-tv", "ytd:xyz", 10990100, 1)
        );
        // A different room on the same map (another channel) does not share that pause.
        Assert.DoesNotContain("paused:", assignments.Resolve("room-tv", "ytd:xyz", 10990100, 2));
        // Freshly setting the same id restarts that room's TV, even mid-playback.
        assignments.SetMovie(10990100, 1, "ytd:xyz");
        Assert.EndsWith(
            $"start:{t0 + 17} offset:0",
            assignments.Resolve("room-tv", "ytd:xyz", 10990100, 1)
        );
        Assert.True(assignments.ControlMovie(10990100, 1, "pattern:vod", "pause"));
        Assert.EndsWith(
            $"paused:{t0 + 17}",
            assignments.Resolve("room-tv", "pattern:vod", 10990100, 1)
        );
        Assert.False(assignments.ControlMovie(10990100, 1, "pattern:live", "pause"));
        Assert.False(assignments.ControlMovie(10990100, 1, "yt-dlp:https://x/y", "pause"));
        Assert.False(assignments.ControlMovie(10990100, 1, "twl:someone", "pause"));
        // SetMovie ignores anything that is not a video: no timeline appears for it.
        assignments.SetMovie(10990100, 1, "twl:someone");
        Assert.Equal(
            "streamlink:https://twitch.tv/someone",
            assignments.Resolve("room-tv", "twl:someone", 10990100, 1)
        );
    }

    [Fact]
    public void TownScreens_FollowTheMapAssignment_UntilCleared()
    {
        var assignments = new ScreenAssignments();
        // Unassigned town screens show the title card.
        Assert.Equal("title", assignments.Resolve("channel-screen", null, 10990100));

        assignments.Set(10990100, "twl:someone https://x/banner");
        // Town screens also get the map's screen position for rolloff, unless the source names one.
        Assert.EndsWith(
            " rolloff:-17340/375/-20639/1000/12000/1/0",
            assignments.Resolve("channel-screen", null, 10990100)
        );
        // The short form keeps the map's position and only sets the range; rolloff:flat turns
        // the rolloff off; a short form on a map without a known screen is dropped.
        assignments.Set(10990100, "twl:someone rolloff:500/6000");
        Assert.Equal(
            "streamlink:https://twitch.tv/someone rolloff:-17340/375/-20639/500/6000/1/0",
            assignments.Resolve("channel-screen", null, 10990100)
        );
        // Four numbers add the gains to fade between, keeping the map's position.
        assignments.Set(10990100, "twl:someone rolloff:500/6000/0.8/0.25");
        Assert.Equal(
            "streamlink:https://twitch.tv/someone rolloff:-17340/375/-20639/500/6000/0.8/0.25",
            assignments.Resolve("channel-screen", null, 10990100)
        );
        assignments.Set(10990100, "twl:someone rolloff:flat");
        Assert.Equal(
            "streamlink:https://twitch.tv/someone",
            assignments.Resolve("channel-screen", null, 10990100)
        );
        assignments.Set(30000001, "twl:someone rolloff:500/6000");
        Assert.Equal(
            "streamlink:https://twitch.tv/someone",
            assignments.Resolve("channel-screen", null, 30000001)
        );
        assignments.Set(10990100, "twl:someone rolloff:1/2/3/10/20");
        Assert.Equal(
            "streamlink:https://twitch.tv/someone rolloff:1/2/3/10/20/1/0",
            assignments.Resolve("channel-screen", null, 10990100)
        );
        // Seven is the hook's own form and passes through unchanged.
        assignments.Set(10990100, "twl:someone rolloff:1/2/3/10/20/0.5/0.1");
        Assert.Equal(
            "streamlink:https://twitch.tv/someone rolloff:1/2/3/10/20/0.5/0.1",
            assignments.Resolve("channel-screen", null, 10990100)
        );
        assignments.Set(10990100, "twl:someone https://x/banner");
        // The page gets the hook's form; the stored assignment keeps the friendly one.
        Assert.Equal(
            "streamlink:https://twitch.tv/someone https://x/banner rolloff:-17340/375/-20639/1000/12000/1/0",
            assignments.Resolve("channel-screen", null, 10990100)
        );
        Assert.Equal(
            "streamlink:https://twitch.tv/someone https://x/banner rolloff:-17340/375/-20639/1000/12000/1/0",
            assignments.Resolve("live-watch", null, 10990100)
        );
        Assert.Equal("twl:someone https://x/banner", assignments.Get(10990100));
        // The typed ids keep their friendly form in the assignment too.
        assignments.Set(10990100, "sm9 pan");
        Assert.Equal("nn:sm9 pan", assignments.Get(10990100));
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
        Assert.True(ScreenAssignments.IsTwitchStreamlinkSource("twl:yueri"));
        Assert.True(ScreenAssignments.IsTwitchEmbedSource("twe:ironmouse"));
        Assert.True(ScreenAssignments.IsBrowserSource("twe:ironmouse"));
        Assert.False(ScreenAssignments.IsTwitchSource("twe:ironmouse"));
        Assert.False(ScreenAssignments.IsTwitchSource("twl:ironmouse"));
        Assert.True(ScreenAssignments.IsYouTubeVideoSource("yt:dQw4w9WgXcQ"));
        Assert.True(ScreenAssignments.IsVideoSource("yt:dQw4w9WgXcQ"));
        Assert.True(ScreenAssignments.IsYouTubeDlpSource("ytd:dQw4w9WgXcQ"));
        Assert.True(ScreenAssignments.IsVideoSource("ytd:dQw4w9WgXcQ"));
        Assert.False(ScreenAssignments.IsYouTubeVideoSource("ytd:dQw4w9WgXcQ"));
        Assert.True(ScreenAssignments.IsYouTubeEmbedSource("yte:dQw4w9WgXcQ"));
        Assert.True(ScreenAssignments.IsVideoSource("yte:dQw4w9WgXcQ"));
        Assert.True(ScreenAssignments.IsBrowserSource("yte:dQw4w9WgXcQ"));
        Assert.False(ScreenAssignments.IsYouTubeVideoSource("yte:dQw4w9WgXcQ"));
        Assert.False(ScreenAssignments.IsYouTubeEmbedSource("yte:"));
        Assert.False(ScreenAssignments.IsYouTubeEmbedSource("yte:bad/id"));
        Assert.True(ScreenAssignments.IsValidChannelContentSource("yte:dQw4w9WgXcQ"));
        Assert.True(ScreenAssignments.IsYouTubeLiveSource("ytl:jfKfPfyJRdk"));
        Assert.False(ScreenAssignments.IsVideoSource("ytl:jfKfPfyJRdk"));
        Assert.False(ScreenAssignments.IsYouTubeVideoSource("ytd:"));
        Assert.False(ScreenAssignments.IsYouTubeVideoSource("ytd:a/b"));
        Assert.True(ScreenAssignments.IsNicoLiveSource("lv351315472"));
        Assert.True(ScreenAssignments.IsNicoVideoSource("sm11273499"));
        Assert.True(ScreenAssignments.IsVideoSource("sm11273499"));
        Assert.True(ScreenAssignments.IsNicoLiveVodId("lv351315472:vod"));
        Assert.False(ScreenAssignments.IsNicoLiveVodId("lv351315472"));
        Assert.True(ScreenAssignments.IsNicoLiveVodSource("lv351315472:vod"));
        Assert.True(ScreenAssignments.IsVideoSource("lv351315472:vod"));
        Assert.False(ScreenAssignments.IsNicoLiveSource("lv351315472:vod"));
        Assert.False(ScreenAssignments.IsNicoLiveId("lvx"));
        Assert.False(ScreenAssignments.IsNicoLiveId("lv"));
        Assert.False(ScreenAssignments.IsNicoVideoId("sm"));
        Assert.False(ScreenAssignments.IsNicoVideoId("smile"));
        Assert.True(ScreenAssignments.IsPatternSource("pattern:live"));
        Assert.True(ScreenAssignments.IsPatternSource("pattern:vod"));
        Assert.True(ScreenAssignments.IsPatternSource("pattern"));
        Assert.False(ScreenAssignments.IsPatternSource("pattern:x"));
        Assert.True(ScreenAssignments.IsVideoSource("pattern:vod"));
        Assert.False(ScreenAssignments.IsVideoSource("pattern:live"));
        foreach (
            var typed in new[]
            {
                "twl:yueri",
                "twe:yueri",
                "ytd:abc",
                "ytl:abc",
                "lv1",
                "sm1",
                "pattern:live",
                "pattern:vod",
            }
        )
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
        Assert.False(ScreenAssignments.IsPageUrl("twl:yueri"));
        // Not sources: the hook's own raw yt-dlp form (yt: and sm… are the typed ways in), other
        // browser hosts, and the raw forms with nothing after the prefix.
        Assert.False(ScreenAssignments.IsValidSource("yt-dlp:https://x/y"));
        Assert.False(ScreenAssignments.IsValidSource("cef:https://x"));
        Assert.False(ScreenAssignments.IsValidSource("edge:https://x"));
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
        Assert.False(ScreenAssignments.IsValidSource("sm"));
        Assert.False(ScreenAssignments.IsValidSource("Hello World"));
        Assert.False(ScreenAssignments.IsValidSource(null));
        // The hook's forms.
        Assert.Equal(
            "streamlink:https://x/y",
            ScreenAssignments.ToHookSource("streamlink:https://x/y")
        );
        Assert.Equal("pattern:live", ScreenAssignments.ToHookSource("Pattern"));
        Assert.Equal("pattern:vod", ScreenAssignments.ToHookSource("pattern:VOD"));
        Assert.Equal("title", ScreenAssignments.ToHookSource("title"));
        Assert.Equal(
            "electron:https://player.twitch.tv/?channel=yueri&parent=aisp.moe scroll:0/40",
            ScreenAssignments.ToHookSource("twe:yueri scroll:0/40")
        );
        Assert.Equal(
            "yt-dlp:https://www.nicovideo.jp/watch/sm9",
            ScreenAssignments.ToHookSource("nnd:sm9")
        );
        Assert.Equal(
            "streamlink:https://live.nicovideo.jp/watch/lv351315472",
            ScreenAssignments.ToHookSource("nnl:lv351315472")
        );
        Assert.Equal(
            "electron:https://www.nicovideo.jp/watch/sm9 run:/ai-sp/run/nicovideo-player.js",
            ScreenAssignments.ToHookSource("sm9")
        );
        // A caller's own run: word stands in for the live page's default one.
        Assert.Equal(
            "electron:https://live.nicovideo.jp/watch/lv351315472 run:https://x/mine.js key",
            ScreenAssignments.ToHookSource("lv351315472 run:https://x/mine.js key")
        );
        Assert.True(ScreenAssignments.IsRunWord("run:/ai-sp/run/nicolive-player.js"));
        Assert.True(ScreenAssignments.IsRunWord("run:https://x/a.js"));
        Assert.False(ScreenAssignments.IsRunWord("run:"));
        Assert.False(ScreenAssignments.IsRunWord("run://x"));
        Assert.False(ScreenAssignments.IsRunWord("run:ftp://x"));
        Assert.True(ScreenAssignments.IsValidSource("electron:https://x/ run:/ai-sp/run/a.js"));
        Assert.False(ScreenAssignments.IsValidSource("electron:https://x/ run:a.js"));
        Assert.Equal(
            "yt-dlp:https://www.nicovideo.jp/watch/lv351315472",
            ScreenAssignments.ToHookSource("lv351315472:vod")
        );
        Assert.Equal(
            "streamlink:https://www.youtube.com/watch?v=abc",
            ScreenAssignments.ToHookSource("ytl:abc")
        );
        // A bare second URL is the raw form (a banner page, or with a box a whole-crop frame
        // page); main:<url> and banner:<url> name the panel. All three have to be pages.
        Assert.True(ScreenAssignments.IsValidSource("twl:yueri https://example.test/banner"));
        Assert.True(ScreenAssignments.IsValidSource("blank https://example.test/banner"));
        Assert.True(
            ScreenAssignments.IsValidSource("twl:yueri banner:https://example.test/banner")
        );
        Assert.True(
            ScreenAssignments.IsValidSource(
                "twe:ironmouse box:40/30/406/240 key main:https://example.test/frame.html banner:https://example.test/top"
            )
        );
        Assert.True(ScreenAssignments.IsMainWord("main:http://x/"));
        Assert.False(ScreenAssignments.IsMainWord("main:x"));
        Assert.False(ScreenAssignments.IsValidSource("twl:yueri main:frame.html"));
        Assert.False(ScreenAssignments.IsValidSource("twl:yueri banner:ftp://x"));
        Assert.False(ScreenAssignments.IsValidSource("twl:yueri twitch:other"));
        Assert.Equal(
            "streamlink:https://twitch.tv/yueri box:0/0/10/10 main:https://x/f",
            ScreenAssignments.ToHookSource("twl:yueri box:0/0/10/10 main:https://x/f")
        );
        // A box:x/y/w/h word places the video inside the crop; the page can put HTML around it.
        // Slashes because the client splits chat arguments on commas.
        Assert.True(ScreenAssignments.IsValidSource("twl:yueri box:20/20/446/303"));
        Assert.True(ScreenAssignments.IsValidSource("twl:yueri https://x/ box:0/76/635/441"));
        Assert.False(ScreenAssignments.IsValidSource("twl:yueri box:20/20"));
        Assert.False(ScreenAssignments.IsValidSource("twl:yueri box:20,20,446,303"));
        // key / key:RRGGBB colour-keys the video into the page's own pixels.
        Assert.True(
            ScreenAssignments.IsValidSource("twl:yueri box:20/20/446/303 key https://x/frame")
        );
        Assert.True(ScreenAssignments.IsValidSource("twl:yueri key:100010"));
        Assert.False(ScreenAssignments.IsValidSource("twl:yueri key:12"));
        Assert.False(ScreenAssignments.IsValidSource("twl:yueri key:zzzzzz"));
        // crop:sw/sh:cx/cy renders at sw x sh and shows the box-sized window at cx,cy of it.
        Assert.True(ScreenAssignments.IsValidSource("twl:yueri crop:972/686:243/171"));
        Assert.True(
            ScreenAssignments.IsValidSource("twl:yueri box:20/20/446/303 crop:892/606:0/0")
        );
        Assert.False(ScreenAssignments.IsValidSource("twl:yueri crop:972/686"));
        // extend:l/t/r/b is the crop worked out from the box by the page.
        Assert.True(ScreenAssignments.IsValidSource("electron:https://x/y extend:0/0/0/200"));
        Assert.True(ScreenAssignments.IsExtendWord("extend:10/20/30/40"));
        Assert.False(ScreenAssignments.IsExtendWord("extend:10/20/30"));
        Assert.False(ScreenAssignments.IsExtendWord("extend:-1/0/0/0"));
        Assert.False(ScreenAssignments.IsValidSource("electron:https://x/y extend:a/b/c/d"));
        Assert.False(ScreenAssignments.IsValidSource("twl:yueri crop:0/686:0/0"));
        Assert.False(ScreenAssignments.IsValidSource("twl:yueri crop:972/686:-1/0"));
        Assert.False(ScreenAssignments.IsValidSource("twl:yueri crop:972,686:0,0"));
        // fps:N picks ffmpeg's constant output rate, from the set that maps to whole samples.
        Assert.True(ScreenAssignments.IsValidSource("twl:yueri fps:60"));
        Assert.False(ScreenAssignments.IsValidSource("twl:yueri fps:24"));
        Assert.True(ScreenAssignments.IsValidSource("twl:yueri pan"));
        var panned = new ScreenAssignments();
        panned.Set(19001003, "twl:yueri pan");
        Assert.Equal(
            "streamlink:https://twitch.tv/yueri pan rolloff:0/352.1/1567/1000/12000/1/0",
            panned.Resolve("channel-screen", null, 19001003)
        );
        Assert.True(
            ScreenAssignments.IsValidSource("twl:yueri rolloff:-17340/375/-20639/1000/12000")
        );
        Assert.True(
            ScreenAssignments.IsValidSource("twl:yueri rolloff:-17340/375/-20639/1000/12000/1/0.2")
        );
        Assert.False(ScreenAssignments.IsValidSource("twl:yueri rolloff:1/2/3"));
        Assert.False(ScreenAssignments.IsValidSource("twl:yueri rolloff:1/2/3/4/5/6"));
        Assert.True(ScreenAssignments.IsValidSource("twl:yueri rolloff:500/6000"));
        Assert.True(ScreenAssignments.IsValidSource("twl:yueri rolloff:500/6000/1/0.3"));
        Assert.True(ScreenAssignments.IsValidSource("twl:yueri rolloff:flat"));
        Assert.False(ScreenAssignments.IsValidSource("twl:yueri audio:500/6000"));
        Assert.Null(ScreenAssignments.DefaultRolloffWord(30000001));
        Assert.Equal(
            "streamlink:https://twitch.tv/yueri box:20/20/446/303",
            ScreenAssignments.ToHookSource("twl:yueri box:20/20/446/303")
        );
        Assert.Equal(
            "twl:yueri https://x/",
            ScreenAssignments.Normalize(" twl:yueri  https://x/ ")
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

    [Fact]
    public void NicotvTag_IsFoundAsAnExtraAndAsTheWholeMovieId()
    {
        // The server appends n:<id> to a TV's movie id; with no typed movie the tag is the whole
        // id, which is the movieid= a powered-off-and-on channel TV comes back with.
        Assert.True(ScreenAssignments.TryGetNicotvId("channel:2 n:9", out var tagged));
        Assert.Equal(9u, tagged);
        Assert.True(ScreenAssignments.TryGetNicotvId("n:3", out var alone));
        Assert.Equal(3u, alone);
        Assert.False(ScreenAssignments.TryGetNicotvId("sm11273499", out _));
        Assert.False(ScreenAssignments.TryGetNicotvId("n:", out _));
        Assert.False(ScreenAssignments.TryGetNicotvId(null, out _));
    }

    [Fact]
    public void Channels_AreSharedByNumber_AndIndirectFromRoomTvsAndMaps()
    {
        // Livestreams and videos are valid channel content; another channel (no indirection
        // chains) is not.
        Assert.True(ScreenAssignments.IsValidChannelContentSource("twl:someone"));
        Assert.True(ScreenAssignments.IsValidChannelContentSource("twe:someone"));
        Assert.True(ScreenAssignments.IsValidChannelContentSource("ytl:abc"));
        Assert.True(ScreenAssignments.IsValidChannelContentSource("lv351315472"));
        Assert.True(ScreenAssignments.IsValidChannelContentSource("streamlink:https://x/y"));
        Assert.True(ScreenAssignments.IsValidChannelContentSource("pattern:live"));
        // A web page is channel content too (the help has always said so): the screen page shows
        // it over the video box, the same as a page given to /screen directly.
        Assert.True(ScreenAssignments.IsValidChannelContentSource("https://example.com/rain.html"));
        Assert.True(ScreenAssignments.IsValidChannelContentSource("ytd:dQw4w9WgXcQ"));
        Assert.True(ScreenAssignments.IsValidChannelContentSource("sm11273499"));
        Assert.True(ScreenAssignments.IsValidChannelContentSource("lv351315472:vod"));
        Assert.True(ScreenAssignments.IsValidChannelContentSource("pattern:vod"));
        Assert.False(ScreenAssignments.IsValidChannelContentSource("channel:2"));
        Assert.False(ScreenAssignments.IsValidChannelContentSource("blank"));
        // A channel is purely a source map: framing belongs on whoever references it, not here.
        Assert.False(ScreenAssignments.IsValidChannelContentSource("twl:someone box:0/0/10/10"));
        Assert.False(ScreenAssignments.IsValidChannelContentSource("twl:someone key"));
        Assert.False(
            ScreenAssignments.IsValidChannelContentSource("https://example.com/rain.html key")
        );

        var assignments = new ScreenAssignments();

        // Unassigned: a room TV's channel button and a map bound to it both show the title card.
        Assert.Null(assignments.GetChannelSource(2));
        Assert.Equal("title", assignments.Resolve("room-tv", "channel:2 n:9", null));
        assignments.Set(10990100, "channel:2");
        Assert.Equal("title", assignments.Resolve("channel-screen", null, 10990100));

        // Assigning the channel is what both a room TV tuned to it and a map bound to it follow,
        // with no further wiring: it is the same channel:2 word either way.
        assignments.SetChannelSource(2, "twl:someone");
        // Normalized to its long form, like any other assignment.
        Assert.Equal("twl:someone", assignments.GetChannelSource(2));
        // A live source drops any extras on the reference (n: included), same as any other
        // typed, non-video room-tv source already does: it needs no shared timeline to key.
        Assert.Equal(
            "streamlink:https://twitch.tv/someone",
            assignments.Resolve("room-tv", "channel:2 n:9", null)
        );
        Assert.Equal(
            "streamlink:https://twitch.tv/someone "
                + ScreenAssignments.DefaultRolloffWord(10990100),
            assignments.Resolve("channel-screen", null, 10990100)
        );

        // Framing extras on the reference (/screen channel:2 box:... key main:...) survive
        // indirection: only the channel: word is swapped for the channel's own main token, so
        // the same channel can be framed differently on different screens.
        assignments.Set(
            10990200,
            "channel:2 box:40/30/406/240 key main:https://example.com/vnframe.html"
        );
        Assert.Equal(
            "streamlink:https://twitch.tv/someone box:40/30/406/240 key main:https://example.com/vnframe.html "
                + ScreenAssignments.DefaultRolloffWord(10990200),
            assignments.Resolve("channel-screen", null, 10990200)
        );

        // GetMapsBoundToChannel finds every map bound to it, and only that channel.
        assignments.Set(19001003, "channel:2");
        assignments.Set(30000001, "channel:3");
        Assert.Equal(
            [10990100u, 10990200u, 19001003u],
            assignments.GetMapsBoundToChannel(2).Order()
        );
        Assert.Equal([30000001u], assignments.GetMapsBoundToChannel(3));

        // Clearing it falls back to the title card again on both sides.
        Assert.True(assignments.ClearChannelSource(2));
        Assert.Null(assignments.GetChannelSource(2));
        Assert.Equal("title", assignments.Resolve("room-tv", "channel:2 n:9", null));
        Assert.Equal("title", assignments.Resolve("channel-screen", null, 10990100));
    }

    [Fact]
    public void ChannelVideos_LoopFromWhenTheChannelWasSet_OnOneTimelineForEveryFollower()
    {
        var time = new TestTime();
        var assignments = new ScreenAssignments(time);
        var t0 = time.Now.ToUnixTimeSeconds();
        assignments.Set(10990100, "channel:2");
        time.Now = time.Now.AddSeconds(20);
        assignments.SetChannelSource(2, "ytd:abc123");
        // The channel's timeline (from when it was set, not from when the map was bound) reaches
        // a room TV tuned to it and a map bound to it alike; the map's framing extras stay.
        Assert.Equal(
            $"yt-dlp:https://www.youtube.com/watch?v=abc123 start:{t0 + 20} offset:0",
            assignments.Resolve("room-tv", "channel:2 n:9", null)
        );
        Assert.Equal(
            $"yt-dlp:https://www.youtube.com/watch?v=abc123 {ScreenAssignments.DefaultRolloffWord(10990100)} start:{t0 + 20} offset:0",
            assignments.Resolve("channel-screen", null, 10990100)
        );
        // No pause or seek for a channel: /screen's controls act on a map's own video only.
        Assert.False(assignments.Control(10990100, "pause"));
        Assert.EndsWith(
            $"start:{t0 + 20} offset:0",
            assignments.Resolve("channel-screen", null, 10990100)
        );
        // The embed carries it in its page URL too.
        assignments.SetChannelSource(3, "yte:abc123");
        time.Now = time.Now.AddSeconds(5);
        Assert.Equal(
            $"electron:/ai-sp/yt-embed?v=abc123&start={t0 + 20}&offset=0 start:{t0 + 20} offset:0",
            assignments.Resolve("room-tv", "channel:3 n:9", null)
        );
        // Setting the channel again restarts its video from now.
        assignments.SetChannelSource(2, "ytd:abc123");
        Assert.EndsWith(
            $"start:{t0 + 25} offset:0",
            assignments.Resolve("room-tv", "channel:2 n:9", null)
        );
    }

    [Fact]
    public void ChannelAuto_FollowsTheRequestingScreensOwnTvid_ButNeverOverridesAnExplicitSource()
    {
        Assert.True(ScreenAssignments.IsValidSource("channel:auto"));
        Assert.True(ScreenAssignments.IsChannelSource("channel:auto"));

        var assignments = new ScreenAssignments();
        assignments.SetChannelSource(1, "twl:one");

        // No tvid= on the request at all: an unassigned map shows the title card, for the routes
        // and requests that never carry one (live-watch, screen, or a channel-screen request
        // without it).
        Assert.Equal("title", assignments.Resolve("channel-screen", null, 40000001));

        // An unassigned map defaults to channel:auto: it follows the requesting screen's own
        // tvid=, without needing an explicit /screen channel:auto first. (These map ids have no
        // registered screen position, so no default rolloff word is added either way here.)
        Assert.Equal(
            "streamlink:https://twitch.tv/one",
            assignments.Resolve("channel-screen", null, 40000001, requestTvId: 1)
        );
        // A requesting tvid= for an unassigned channel still falls back to the title card.
        Assert.Equal(
            "title",
            assignments.Resolve("channel-screen", null, 40000001, requestTvId: 5)
        );

        // Explicit /screen channel:auto behaves identically to the unassigned default.
        assignments.Set(40000002, "channel:auto");
        Assert.Equal(
            "streamlink:https://twitch.tv/one",
            assignments.Resolve("channel-screen", null, 40000002, requestTvId: 1)
        );

        // An explicit, non-channel /screen assignment always wins over the requesting screen's
        // own tvid=: a moderator's direct assignment is authoritative, and a town screen's own
        // tvid= must never override it.
        assignments.Set(40000003, "twl:two");
        Assert.Equal(
            "streamlink:https://twitch.tv/two",
            assignments.Resolve("channel-screen", null, 40000003, requestTvId: 1)
        );
    }

    [Fact]
    public void ReloadIds_ScopeTheHookToOneChannel_OrToEveryScreen()
    {
        Assert.Equal("lv0", ScreenAssignments.ReloadForChannel(0));
        Assert.Equal("lv99", ScreenAssignments.ReloadForChannel(99));
        Assert.Equal("lv100", ScreenAssignments.ReloadEveryScreen);
        Assert.Equal("lv200", ScreenAssignments.ReloadEveryScreenHard);
        Assert.Equal(ScreenAssignments.ReloadEveryScreen, ScreenAssignments.ReloadForChannel(100));
        Assert.Equal(ScreenAssignments.ReloadEveryScreen, ScreenAssignments.ReloadForChannel(1234));
    }

    [Fact]
    public void FollowsChannel_TellsBoundMaps_FromAutoOnes_FromTheRest()
    {
        var assignments = new ScreenAssignments();
        assignments.Set(2, "channel:3");
        assignments.Set(3, "channel:auto");
        assignments.Set(4, "channel:4 key");
        assignments.Set(5, "tw:someone");
        Assert.Equal(ScreenAssignments.ChannelFollowing.Auto, assignments.FollowsChannel(1, 3));
        Assert.Equal(ScreenAssignments.ChannelFollowing.Bound, assignments.FollowsChannel(2, 3));
        Assert.Equal(ScreenAssignments.ChannelFollowing.None, assignments.FollowsChannel(2, 4));
        Assert.Equal(ScreenAssignments.ChannelFollowing.Auto, assignments.FollowsChannel(3, 3));
        Assert.Equal(ScreenAssignments.ChannelFollowing.Bound, assignments.FollowsChannel(4, 4));
        Assert.Equal(ScreenAssignments.ChannelFollowing.None, assignments.FollowsChannel(5, 3));
    }
}
