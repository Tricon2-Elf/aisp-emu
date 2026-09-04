using System.Collections.Concurrent;
using System.Globalization;

namespace aisp.Common.Game;

/// <summary>
/// What the in-game screens on a map should play. The client's town displays (the Akihabara
/// screen, the Stage billboard) and room TVs are IE controls that load a page from the emulator
/// (see ScreenEndpointsExtensions); the page tells the launcher's hook what to stream by
/// publishing a source in its title, and this is where that source comes from. One source per
/// map, set with the /screen chat command.
///
/// The ids anyone may type into a room TV (fetched by every viewer's client, so only short ids
/// that expand to a known site): title, pattern:live, pattern:vod, tw:&lt;channel&gt; (Twitch
/// through streamlink), twe:&lt;channel&gt; (the Twitch player embed in the off-screen browser),
/// yt:&lt;id&gt; (a YouTube video through yt-dlp), ytl:&lt;id&gt; (a YouTube live through
/// streamlink), lv… (Nico Live through streamlink), lv…:vod (the same, once archived, as a
/// video through yt-dlp instead) and sm… (a Nico video through yt-dlp).
/// Only /screen takes streamlink:&lt;url&gt;, stream:&lt;url&gt;, electron:&lt;url&gt; and a
/// web page URL.
/// </summary>
public sealed class ScreenAssignments(TimeProvider? time = null)
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private readonly ConcurrentDictionary<uint, Entry> _byMap = new();

    // Timelines of videos typed into room TVs, by the furniture's own database id where known
    // (see the n: tag below), or by map and channel otherwise: room instances share a map id,
    // and the channel the hook adds to the TV's URL is not reliably one room's own (it is the
    // player's session slot, not the room), so that id is what actually distinguishes rooms.
    private readonly ConcurrentDictionary<string, Timeline> _byMovie = new(
        StringComparer.OrdinalIgnoreCase
    );

    private static string MovieKey(uint map, int channel, string movieId) =>
        string.Create(CultureInfo.InvariantCulture, $"{map}/{channel}/{movieId}");

    private static string NicotvKey(uint nicotvId) =>
        string.Create(CultureInfo.InvariantCulture, $"nicotv:{nicotvId}");

    /// <summary>
    /// n:&lt;id&gt;: the Nicotv furniture's own database id, carried as a tag on a room TV's
    /// movie id so the shared timeline can be keyed by the specific TV rather than by map
    /// and channel, which do not reliably identify a room (see <see cref="_byMovie"/>). Short
    /// (not nicotvid:) since it is invisible wire budget inside a 96-character movie id, not
    /// something read by a person.
    /// </summary>
    public static bool TryGetNicotvId(string? source, out uint nicotvId)
    {
        nicotvId = 0;
        if (source is null)
            return false;
        foreach (var word in ExtrasOf(source))
            if (
                word.StartsWith("n:", StringComparison.OrdinalIgnoreCase)
                && uint.TryParse(
                    word.AsSpan(2),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out nicotvId
                )
            )
                return true;
        return false;
    }

    /// <summary>The room-tv timeline key for a (possibly n:-tagged) movie id: the furniture's
    /// own id when present, otherwise the map-and-channel fallback.</summary>
    private static string RoomTvKey(uint map, int channel, string movieId) =>
        TryGetNicotvId(movieId, out var nicotvId)
            ? NicotvKey(nicotvId)
            : MovieKey(map, channel, MainOf(movieId));

    /// <summary>
    /// Where a video is: at <see cref="StartedAt"/> it was at <see cref="Offset"/> seconds and
    /// playing, unless <see cref="PausedAt"/> is set, in which case it has sat at the position
    /// it reached then. Every client computes the same position from the same numbers, so all
    /// screens show the same point of the video; the page publishes them for the hook, which
    /// seeks accordingly and holds while paused.
    /// </summary>
    public sealed record Timeline(DateTimeOffset StartedAt, double Offset, DateTimeOffset? PausedAt)
    {
        public bool Paused => PausedAt is not null;

        public double PositionAt(DateTimeOffset now) =>
            Math.Max(0, Offset + ((PausedAt ?? now) - StartedAt).TotalSeconds);

        public Timeline Pause(DateTimeOffset now) => Paused ? this : this with { PausedAt = now };

        public Timeline Resume(DateTimeOffset now) =>
            Paused ? new Timeline(now, PositionAt(now), null) : this;

        public Timeline Seek(DateTimeOffset now, double seconds) =>
            new(now, Math.Max(0, seconds), Paused ? now : null);
    }

    private sealed record Entry(string Source, Timeline Timeline);

    public void Set(uint mapId, string source) =>
        _byMap[mapId] = new Entry(Normalize(source), new Timeline(_time.GetUtcNow(), 0, null));

    public Timeline? GetTimeline(uint mapId) =>
        _byMap.TryGetValue(mapId, out var entry) ? entry.Timeline : null;

    /// <summary>pause, resume or seek:&lt;seconds&gt; on the map's video; false without one.</summary>
    public bool Control(uint mapId, string action)
    {
        if (!_byMap.TryGetValue(mapId, out var entry) || !IsVideoSource(entry.Source))
            return false;
        var updated = Apply(entry.Timeline, action);
        if (updated is null)
            return false;
        _byMap[mapId] = entry with { Timeline = updated };
        return true;
    }

    /// <summary>
    /// The same for a video typed into a room TV (its timeline is shared by everyone on that TV,
    /// in that room: map plus channel, since room instances reuse the same map id).
    /// </summary>
    public bool ControlMovie(uint map, int channel, string movieId, string action)
    {
        if (!IsVideoSource(movieId))
            return false;
        var key = RoomTvKey(map, channel, movieId);
        var timeline = _byMovie.GetOrAdd(key, _ => new Timeline(_time.GetUtcNow(), 0, null));
        var updated = Apply(timeline, action);
        if (updated is null)
            return false;
        _byMovie[key] = updated;
        return true;
    }

    /// <summary>
    /// A room TV was freshly set to a movie: (re)starts that TV's timeline at zero for the room,
    /// so picking a video plays it from the beginning even if that id was already playing
    /// somewhere else. A no-op for anything that is not a video (streams, pages, the pattern).
    /// </summary>
    public void SetMovie(uint map, int channel, string movieId)
    {
        if (!IsVideoSource(movieId))
            return;
        _byMovie[RoomTvKey(map, channel, movieId)] = new Timeline(_time.GetUtcNow(), 0, null);
    }

    private Timeline? Apply(Timeline timeline, string action)
    {
        var now = _time.GetUtcNow();
        if (string.Equals(action, "pause", StringComparison.OrdinalIgnoreCase))
            return timeline.Pause(now);
        if (string.Equals(action, "resume", StringComparison.OrdinalIgnoreCase))
            return timeline.Resume(now);
        if (
            action.StartsWith("seek:", StringComparison.OrdinalIgnoreCase)
            && double.TryParse(
                action[5..],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var seconds
            )
        )
            return timeline.Seek(now, seconds);
        return null;
    }

    private static string TimelineWords(Timeline timeline) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"start:{timeline.StartedAt.ToUnixTimeSeconds()} offset:{timeline.Offset:0.###}"
        ) + (timeline.Paused ? " paused:" + timeline.PausedAt!.Value.ToUnixTimeSeconds() : "");

    /// <summary>An empty main area; used with a banner page on the Stage wall.</summary>
    public const string Blank = "blank";

    /// <summary>The diagnostic page itself, whatever the map is set to.</summary>
    public const string TestScreen = "testscreen";

    /// <summary>A plain title card, "aisp-emu" on a dark background: the default look for a display.</summary>
    public const string TitleCard = "title";

    /// <summary>The diagnostic page with a fine coordinate grid, for measuring screen panels.</summary>
    public const string Calibrate = "calibrate";

    /// <summary>
    /// The launcher hook's own calibration picture and test tone, generated in the client at the
    /// panel's exact size. pattern:live counts from zero whenever it starts, like a stream;
    /// pattern:vod is a looping video that follows the shared timeline, so pause, resume and
    /// seek can be checked without a real video. Bare "pattern" is the live one.
    /// </summary>
    public const string PatternLive = "pattern:live";
    public const string PatternVod = "pattern:vod";

    /// <summary>
    /// The parent Twitch's player embed demands: it must name the site embedding the player.
    /// The off-screen browser loads the player page top-level, so the value only has to exist.
    /// </summary>
    public const string TwitchEmbedParent = "aisp.moe";

    /// <summary>
    /// The map with the Nico Live billboard (seedData/maps.json: "Stage"). notify_nicolive_reload
    /// only does anything there (confirmed on the shopping mall, which has none): the client has
    /// nothing on other maps that reacts to it, so sending it elsewhere is a silent no-op.
    /// CmdExecHandler scopes its send to this map.
    /// </summary>
    public const uint StageMapId = 19_001_003;

    /// <summary>
    /// Canonical form of a source: trimmed, with the typed short ids expanded to their prefixed
    /// forms (tw: to twitch:, a bare lv… or sm… id to nico:, bare pattern to pattern:live). A
    /// source is "&lt;main&gt; [main:&lt;url&gt;] [banner:&lt;url&gt;] [&lt;url&gt;] [box:x/y/w/h]
    /// [crop:sw/sh:cx/cy] [scrollx:N] [scrolly:N] [scroll:x/y] [scale:N] [key[:RRGGBB]] [fps:N]
    /// [rolloff:…] [pan]": main:&lt;url&gt; is a frame page under the main panel, with the box
    /// relative to that panel; banner:&lt;url&gt; is a page for the Stage banner strip (the
    /// title card when absent); a bare page URL is the raw form, a frame page under the whole
    /// crop with a box or key word, the banner otherwise; a box word places the video inside
    /// the crop; a crop word renders the picture at another size and shows the box-sized window
    /// at cx,cy of it (for browser sources sw/sh is the layout viewport); scroll words pan a
    /// browser document; scale:N is browser zoom; a key word colour-keys the video into the page.
    /// </summary>
    public static string Normalize(string source)
    {
        var words = source.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return "";
        var main = words[0];
        if (main.StartsWith("tw:", StringComparison.OrdinalIgnoreCase))
            main = "twitch:" + main[3..];
        else if (IsNicoLiveId(main) || IsNicoVideoId(main) || IsNicoLiveVodId(main))
            main = "nico:" + main;
        else if (string.Equals(main, "pattern", StringComparison.OrdinalIgnoreCase))
            main = PatternLive;
        else if (
            string.Equals(main, PatternLive, StringComparison.OrdinalIgnoreCase)
            || string.Equals(main, PatternVod, StringComparison.OrdinalIgnoreCase)
        )
            main = main.ToLowerInvariant();
        return string.Join(' ', new[] { main }.Concat(words.Skip(1)));
    }

    /// <summary>
    /// key or key:RRGGBB: colour keying. The page paints the key colour where video belongs and
    /// the hook fills only those pixels, so HTML can sit over the video. Bare "key" is the
    /// DirectDraw overlay key of the era, the near-black RGB 16,0,16 (100010) that let a
    /// rectangle painted in Paint show the movie through.
    /// </summary>
    public static bool IsKeyWord(string? word) =>
        word is not null
        && (
            string.Equals(word, "key", StringComparison.OrdinalIgnoreCase)
            || (
                word.StartsWith("key:", StringComparison.OrdinalIgnoreCase)
                && word.Length == 10
                && word[4..].All(char.IsAsciiHexDigit)
            )
        );

    /// <summary>
    /// rolloff:near/far (range only, the map's own screen position), rolloff:near/far/max/min
    /// (also the gains to fade between), rolloff:x/y/z/near/far, rolloff:x/y/z/near/far/max/min,
    /// or rolloff:flat (flat volume). max is the gain at near or closer and min the gain at far
    /// or beyond, so the <see cref="DefaultMax"/>/<see cref="DefaultMin"/> default is the plain
    /// fade to silence and e.g. 1/0.3 keeps a screen audible across the map. The hook attenuates
    /// and pans the stream from the player's own position; town screens get a default from
    /// <see cref="ScreenPositions"/> when no word is given.
    /// </summary>
    public static bool IsRolloffWord(string? word) =>
        word is not null
        && word.StartsWith("rolloff:", StringComparison.OrdinalIgnoreCase)
        && (
            string.Equals(word, "rolloff:flat", StringComparison.OrdinalIgnoreCase)
            || (
                word[8..].Split('/') is { Length: 2 or 4 or 5 or 7 } parts
                && parts.All(part =>
                    double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
                )
            )
        );

    /// <summary>
    /// The hook wants the seven-number form: fills in the map's screen position for the forms
    /// that leave it out and <see cref="DefaultMax"/>/<see cref="DefaultMin"/> for the forms that
    /// leave the gains out. Drops rolloff:flat, or a position-less word on a map without a screen.
    /// </summary>
    private static string? ExpandRolloffWord(string word, uint? mapId)
    {
        if (string.Equals(word, "rolloff:flat", StringComparison.OrdinalIgnoreCase))
            return null;
        var parts = word[8..].Split('/');
        // 5 and 7 carry the position; 2 and 4 take the map's own screen, and have nowhere to
        // fall back to without one.
        var positioned = parts.Length is 5 or 7;
        string position;
        if (positioned)
            position = $"{parts[0]}/{parts[1]}/{parts[2]}";
        else if (mapId is { } map && ScreenPositions.TryGetValue(map, out var p))
            position = string.Create(CultureInfo.InvariantCulture, $"{p.X}/{p.Y}/{p.Z}");
        else
            return null;
        var range = positioned ? $"{parts[3]}/{parts[4]}" : $"{parts[0]}/{parts[1]}";
        // 4 and 7 carry the gains; the other two fade all the way to silence.
        var gains = parts.Length switch
        {
            4 => $"{parts[2]}/{parts[3]}",
            7 => $"{parts[5]}/{parts[6]}",
            _ => string.Create(CultureInfo.InvariantCulture, $"{DefaultMax}/{DefaultMin}"),
        };
        return $"rolloff:{position}/{range}/{gains}";
    }

    /// <summary>
    /// World positions of the town screens, from the client's screen table (one record per map;
    /// maps with several screens list the first). Room TVs have no fixed position. Same units and
    /// axes as the avatar position the client reports in move packets (and reads from its own
    /// CChara transform for the launcher hook's rolloff), so the distance is plain Euclidean.
    /// </summary>
    public static readonly IReadOnlyDictionary<uint, (float X, float Y, float Z)> ScreenPositions =
        new Dictionary<uint, (float, float, float)>
        {
            [10990100] = (-17340f, 375f, -20639f),
            [10990110] = (-16850f, 310f, -19840f),
            [10990200] = (-10873f, 1202.5f, -997f),
            [10990210] = (-10873f, 1202.5f, -997f),
            [10990220] = (-10873f, 1202.5f, -997f),
            [19001003] = (0f, 352.1f, 1567f),
            [10010200] = (-32.6f, 497f, -2086.4f),
            [10010210] = (-32.6f, 497f, -2086.4f),
            [10020200] = (-32.6f, 497f, -2086.4f),
            [10020210] = (-32.6f, 497f, -2086.4f),
            [10030200] = (-32.6f, 497f, -2086.4f),
            [10030210] = (-32.6f, 497f, -2086.4f),
            [10010110] = (-11826f, 150f, -4350f),
            [10990400] = (-239.6f, 274.5f, 1184.8f),
        };

    /// <summary>Default rolloff, in world units: full volume up to Near, silent from Far.</summary>
    public const float DefaultNear = 1000f;
    public const float DefaultFar = 12000f;

    /// <summary>Default gains to fade between: full at Near or closer, silent at Far or beyond.</summary>
    public const float DefaultMax = 1f;
    public const float DefaultMin = 0f;

    /// <summary>The rolloff word for a town screen's map, or null for maps without a known screen.</summary>
    public static string? DefaultRolloffWord(uint mapId) =>
        ScreenPositions.TryGetValue(mapId, out var p)
            ? string.Create(
                CultureInfo.InvariantCulture,
                $"rolloff:{p.X}/{p.Y}/{p.Z}/{DefaultNear}/{DefaultFar}/{DefaultMax}/{DefaultMin}"
            )
            : null;

    /// <summary>fps:N, the constant rate ffmpeg produces: 15, 20, 25, 30, 50 or 60 (the rates
    /// that divide both common sample rates, so the audio clock maps to whole frames).</summary>
    public static bool IsFpsWord(string? word) =>
        word is not null
        && word.StartsWith("fps:", StringComparison.OrdinalIgnoreCase)
        && int.TryParse(word[4..], out var fps)
        && fps is 15 or 20 or 25 or 30 or 50 or 60;

    /// <summary>pan: stereo-pan the stream by its bearing from the player. Off by default; a
    /// screen in a street is not a point source, so only the distance rolloff applies.</summary>
    public static bool IsPanWord(string? word) =>
        string.Equals(word, "pan", StringComparison.OrdinalIgnoreCase);

    /// <summary>scale:N for browser sources: Electron's zoom (1 = 100%), 0.1–8. A texture stretch this is not.</summary>
    public static bool IsScaleWord(string? word) =>
        word is not null
        && word.StartsWith("scale:", StringComparison.OrdinalIgnoreCase)
        && double.TryParse(
            word[6..],
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var scale
        )
        && scale is >= 0.1 and <= 8;

    /// <summary>
    /// scrollx:N, scrolly:N, or scroll:x/y: document offset in CSS pixels, enforced inside the
    /// off-screen browser. The layout viewport is crop:sw/sh when given, else the box.
    /// </summary>
    public static bool IsScrollWord(string? word)
    {
        if (word is null)
            return false;
        if (
            word.StartsWith("scrollx:", StringComparison.OrdinalIgnoreCase)
            || word.StartsWith("scrolly:", StringComparison.OrdinalIgnoreCase)
        )
            return int.TryParse(
                word[8..],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out _
            );
        return word.StartsWith("scroll:", StringComparison.OrdinalIgnoreCase)
            && word[7..].Split('/') is { Length: 2 } parts
            && parts.All(part =>
                int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
            );
    }

    /// <summary>
    /// box:x/y/w/h with four non-negative integers. Slashes, not commas: the client splits chat
    /// arguments on commas and spaces (URLs, with their colons and slashes, come through whole).
    /// </summary>
    public static bool IsBoxWord(string? word) =>
        word is not null
        && word.StartsWith("box:", StringComparison.OrdinalIgnoreCase)
        && word[4..].Split('/') is { Length: 4 } parts
        && parts.All(part => int.TryParse(part, out var n) && n >= 0);

    /// <summary>
    /// crop:sw/sh:cx/cy: the picture is rendered (letterboxed) at sw x sh instead of the box
    /// size, and the box-sized window starting at cx,cy of that is what the box shows. Zooms
    /// in, or cuts a region out: crop:972/686:243/171 shows the middle quarter of a TV. For
    /// browser sources sw x sh is the layout viewport, not a video letterbox.
    /// </summary>
    public static bool IsCropWord(string? word) =>
        word is not null
        && word.StartsWith("crop:", StringComparison.OrdinalIgnoreCase)
        && word[5..].Split(':') is { Length: 2 } halves
        && halves[0].Split('/') is { Length: 2 } size
        && size.All(part => int.TryParse(part, out var n) && n > 0)
        && halves[1].Split('/') is { Length: 2 } origin
        && origin.All(part => int.TryParse(part, out var n) && n >= 0);

    private static string MainOf(string source) => Normalize(source).Split(' ')[0];

    private static IEnumerable<string> ExtrasOf(string source) =>
        Normalize(source).Split(' ').Skip(1);

    public bool Clear(uint mapId) => _byMap.TryRemove(mapId, out _);

    public string? Get(uint mapId) =>
        _byMap.TryGetValue(mapId, out var entry) ? entry.Source : null;

    // The ids end up inside URLs on every viewer's command line (streamlink, yt-dlp, the
    // browser host), so only the characters the sites themselves use are let through.

    /// <summary>A Twitch login: letters, digits and underscores.</summary>
    public static bool IsTwitchChannel(string? value) =>
        value is { Length: > 0 and <= 32 }
        && value.All(c => char.IsAsciiLetterOrDigit(c) || c == '_');

    /// <summary>A YouTube video id: letters, digits, '-' and '_'.</summary>
    public static bool IsYouTubeId(string? value) =>
        value is { Length: > 0 and <= 64 }
        && value.All(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_');

    /// <summary>lv followed by digits: a Nico Live programme id, as typed into a TV.</summary>
    public static bool IsNicoLiveId(string? value) =>
        value is not null
        && value.Length > 2
        && value.StartsWith("lv", StringComparison.OrdinalIgnoreCase)
        && value[2..].All(char.IsAsciiDigit);

    /// <summary>sm followed by digits: a Nico video id, as typed into a TV.</summary>
    public static bool IsNicoVideoId(string? value) =>
        value is not null
        && value.Length > 2
        && value.StartsWith("sm", StringComparison.OrdinalIgnoreCase)
        && value[2..].All(char.IsAsciiDigit);

    /// <summary>
    /// A Nico Live id with :vod appended: once a broadcast has ended and Nico has archived it,
    /// the same watch page also serves it as a plain video, playable through yt-dlp with a
    /// shared timeline (pause/resume/seek) instead of live through streamlink.
    /// </summary>
    public static bool IsNicoLiveVodId(string? value) =>
        value is not null
        && value.EndsWith(":vod", StringComparison.OrdinalIgnoreCase)
        && IsNicoLiveId(value[..^4]);

    private static bool HasPrefix(string main, string prefix, Func<string, bool> rest) =>
        main.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && rest(main[prefix.Length..]);

    /// <summary>twitch:&lt;channel&gt; (tw: for short): a Twitch live stream through streamlink.</summary>
    public static bool IsTwitchSource(string? source) =>
        source is not null && HasPrefix(MainOf(source), "twitch:", IsTwitchChannel);

    /// <summary>twe:&lt;channel&gt;: the Twitch player embed in the off-screen browser.</summary>
    public static bool IsTwitchEmbedSource(string? source) =>
        source is not null && HasPrefix(MainOf(source), "twe:", IsTwitchChannel);

    /// <summary>nico:lv… (a bare lv… id): a Nico Live programme through streamlink.</summary>
    public static bool IsNicoLiveSource(string? source) =>
        source is not null && HasPrefix(MainOf(source), "nico:", IsNicoLiveId);

    /// <summary>nico:lv…:vod (see <see cref="IsNicoLiveVodId"/>): the archive through yt-dlp,
    /// with a shared timeline, instead of live through streamlink.</summary>
    public static bool IsNicoLiveVodSource(string? source) =>
        source is not null && HasPrefix(MainOf(source), "nico:", IsNicoLiveVodId);

    /// <summary>nico:sm… (a bare sm… id): a Nico video through yt-dlp, with a shared timeline.</summary>
    public static bool IsNicoVideoSource(string? source) =>
        source is not null && HasPrefix(MainOf(source), "nico:", IsNicoVideoId);

    /// <summary>yt:&lt;id&gt;: a YouTube video through yt-dlp, with a shared timeline.</summary>
    public static bool IsYouTubeVideoSource(string? source) =>
        source is not null && HasPrefix(MainOf(source), "yt:", IsYouTubeId);

    /// <summary>ytl:&lt;id&gt;: a YouTube live stream through streamlink.</summary>
    public static bool IsYouTubeLiveSource(string? source) =>
        source is not null && HasPrefix(MainOf(source), "ytl:", IsYouTubeId);

    /// <summary>pattern:live or pattern:vod, the hook's own test picture and tone.</summary>
    public static bool IsPatternSource(string? source) =>
        source is not null
        && (
            string.Equals(MainOf(source), PatternLive, StringComparison.OrdinalIgnoreCase)
            || string.Equals(MainOf(source), PatternVod, StringComparison.OrdinalIgnoreCase)
        );

    /// <summary>
    /// electron:&lt;http(s) url&gt;: any page in the off-screen browser, overlaid like a stream.
    /// Only from /screen; a typed room TV id would make every viewer's client fetch the page.
    /// </summary>
    public static bool IsElectronSource(string? source) =>
        source is not null && HasPrefix(MainOf(source), "electron:", IsPageUrl);

    /// <summary>Sources the off-screen browser shows: electron:&lt;url&gt; and the Twitch embed.</summary>
    public static bool IsBrowserSource(string? source) =>
        IsElectronSource(source) || IsTwitchEmbedSource(source);

    /// <summary>A video with a shared timeline (not a live stream): yt:, sm… and pattern:vod.
    /// The hook seeks to the shared position and /screen pause, resume and seek move it.</summary>
    public static bool IsVideoSource(string? source) =>
        IsYouTubeVideoSource(source)
        || IsNicoVideoSource(source)
        || IsNicoLiveVodSource(source)
        || (
            source is not null
            && string.Equals(MainOf(source), PatternVod, StringComparison.OrdinalIgnoreCase)
        );

    /// <summary>
    /// The ids anyone may type into a room TV: short ids that expand to a known site (or the
    /// test pattern), never an arbitrary URL, since every viewer's client fetches what is typed.
    /// </summary>
    public static bool IsTypedSource(string? source) =>
        IsTwitchSource(source)
        || IsTwitchEmbedSource(source)
        || IsNicoLiveSource(source)
        || IsYouTubeLiveSource(source)
        || IsVideoSource(source)
        || IsPatternSource(source);

    /// <summary>
    /// Sources the launcher hook plays: the typed ids, plus (from /screen only)
    /// streamlink:&lt;url&gt; (any page one of streamlink's plugins handles), stream:&lt;url&gt;
    /// (an MP4, HLS playlist, anything ffmpeg opens) and electron:&lt;http(s) url&gt;.
    /// </summary>
    public static bool IsStreamSource(string? source) =>
        source is not null
        && (
            IsTypedSource(source)
            || HasPrefix(MainOf(source), "streamlink:", rest => rest.Length > 0)
            || HasPrefix(MainOf(source), "stream:", rest => rest.Length > 0)
            || IsElectronSource(source)
        );

    /// <summary>main:&lt;url&gt;: a frame page under the main panel; box:x/y/w/h is then relative to it.</summary>
    public static bool IsMainWord(string? word) =>
        word is not null && HasPrefix(word, "main:", IsPageUrl);

    /// <summary>banner:&lt;url&gt;: a page for the Stage wall's banner strip.</summary>
    public static bool IsBannerWord(string? word) =>
        word is not null && HasPrefix(word, "banner:", IsPageUrl);

    /// <summary>A web page the screen shows as is, framed inside the screen page.</summary>
    public static bool IsPageUrl(string? source) =>
        source is not null
        && (
            MainOf(source).StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || MainOf(source).StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        );

    /// <summary>
    /// What /screen accepts: a stream, a page, blank, the title card, the test page, the
    /// calibration aids; with optional extras (main:, banner: and bare page URLs, box, crop, key,
    /// fps, rolloff, pan, scroll and scale words).
    /// </summary>
    public static bool IsValidSource(string? source)
    {
        if (source is null)
            return false;
        var main = MainOf(source);
        if (
            !ExtrasOf(source)
                .All(word =>
                    IsPageUrl(word)
                    || IsMainWord(word)
                    || IsBannerWord(word)
                    || IsBoxWord(word)
                    || IsCropWord(word)
                    || IsKeyWord(word)
                    || IsFpsWord(word)
                    || IsRolloffWord(word)
                    || IsPanWord(word)
                    || IsScrollWord(word)
                    || IsScaleWord(word)
                )
        )
            return false;
        return IsStreamSource(main)
            || IsPageUrl(main)
            || string.Equals(main, Blank, StringComparison.OrdinalIgnoreCase)
            || string.Equals(main, TitleCard, StringComparison.OrdinalIgnoreCase)
            || string.Equals(main, TestScreen, StringComparison.OrdinalIgnoreCase)
            || string.Equals(main, Calibrate, StringComparison.OrdinalIgnoreCase)
            // c:x1/y1:x2/y2[:x3/y3:x4/y4]... draws those rectangles on the page, for measuring.
            || main.StartsWith("c:", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The form the launcher hook understands: streamlink:&lt;url&gt; (anything streamlink
    /// opens), stream:&lt;url&gt; (anything ffmpeg opens), yt-dlp:&lt;url&gt; (a video it seeks),
    /// pattern:live, pattern:vod and electron:&lt;http(s) url&gt;; the friendly ids are
    /// vocabulary of this server. Page URLs and the page's own keywords pass through.
    /// </summary>
    public static string ToHookSource(string source)
    {
        var words = Normalize(source).Split(' ');
        var main = words[0];
        if (IsTwitchSource(main))
            main = "streamlink:https://twitch.tv/" + main[7..];
        else if (IsTwitchEmbedSource(main))
            main =
                "electron:https://player.twitch.tv/?channel="
                + main[4..]
                + "&parent="
                + TwitchEmbedParent;
        else if (IsNicoLiveVodSource(main))
            main = "yt-dlp:https://www.nicovideo.jp/watch/" + main[5..^4];
        else if (IsNicoLiveSource(main))
            main = "streamlink:https://live.nicovideo.jp/watch/" + main[5..];
        else if (IsNicoVideoSource(main))
            main = "yt-dlp:https://www.nicovideo.jp/watch/" + main[5..];
        else if (IsYouTubeVideoSource(main))
            main = "yt-dlp:https://www.youtube.com/watch?v=" + main[3..];
        else if (IsYouTubeLiveSource(main))
            main = "streamlink:https://www.youtube.com/watch?v=" + main[4..];
        return string.Join(' ', new[] { main }.Concat(words.Skip(1)));
    }

    /// <summary>
    /// The source a screen page should publish, given the route the hook sent the client to,
    /// the movie id (room TVs only), the map the hook read from the client and, for a room TV,
    /// the channel that tells its room instance apart from another one on the same map. Null
    /// means the page shows its diagnostics. A typed movie id only reaches this for the typed
    /// ids (<see cref="IsTypedSource"/>), the title card, the test page and the calibration
    /// grid: anything else typed (the c: rectangles included) falls back to the map's
    /// assignment. An unassigned room TV stays blank; an unassigned town screen shows the title
    /// card.
    /// </summary>
    public string? Resolve(string route, string? movieId, uint? mapId, int channel = 0)
    {
        if (route == "room-tv" && movieId is not null)
        {
            var typed = MainOf(movieId);
            if (string.Equals(typed, TestScreen, StringComparison.OrdinalIgnoreCase))
                return null;
            if (string.Equals(typed, TitleCard, StringComparison.OrdinalIgnoreCase))
                return TitleCard;
            if (string.Equals(typed, Calibrate, StringComparison.OrdinalIgnoreCase))
                return Calibrate;
            if (IsVideoSource(typed))
            {
                // A typed video plays from a timeline shared by that TV, in that room.
                var timeline = _byMovie.GetOrAdd(
                    RoomTvKey(mapId ?? 0, channel, movieId),
                    _ => new Timeline(_time.GetUtcNow(), 0, null)
                );
                return ToHookSource(typed) + " " + TimelineWords(timeline);
            }
            if (IsTypedSource(typed))
                return ToHookSource(typed);
        }
        var entry = mapId is { } map && _byMap.TryGetValue(map, out var found) ? found : null;
        if (entry is null)
            return route == "room-tv" ? Blank : TitleCard;
        if (string.Equals(entry.Source, TestScreen, StringComparison.OrdinalIgnoreCase))
            return null;
        var assigned = entry.Source;
        // Town screens attenuate with distance by default; an explicit rolloff word wins, filled
        // out to the hook's seven-number form; rolloff:flat drops it.
        var words = ToHookSource(assigned).Split(' ').ToList();
        var rolloffIndex = words.FindIndex(IsRolloffWord);
        if (rolloffIndex >= 0)
        {
            var expanded = ExpandRolloffWord(words[rolloffIndex], mapId);
            if (expanded is null)
                words.RemoveAt(rolloffIndex);
            else
                words[rolloffIndex] = expanded;
        }
        else if (
            IsStreamSource(assigned)
            && mapId is { } m2
            && DefaultRolloffWord(m2) is { } defaultRolloff
        )
        {
            words.Add(defaultRolloff);
        }
        if (IsVideoSource(assigned))
            words.Add(TimelineWords(entry.Timeline));
        return string.Join(' ', words);
    }
}
