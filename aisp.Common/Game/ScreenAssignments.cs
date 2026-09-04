using System.Collections.Concurrent;

namespace aisp.Common.Game;

/// <summary>
/// What the in-game screens on a map should play. The client's town displays (the Akihabara
/// screen, the Stage billboard) and room TVs are IE controls that load a page from the emulator
/// (see ScreenEndpointsExtensions); the page tells the launcher's hook what to stream by
/// publishing a source in its title, and this is where that source comes from. One source per
/// map, set with the /screen chat command.
///
/// The ids anyone may type into a room TV (fetched by every viewer's client, so only short ids
/// that expand to a known site): title, pattern:live, tw:&lt;channel&gt; (Twitch through
/// streamlink), twe:&lt;channel&gt; (the Twitch player embed in the off-screen browser),
/// ytl:&lt;id&gt; (a YouTube live through streamlink) and lv… (Nico Live through streamlink).
/// Only /screen takes streamlink:&lt;url&gt;, stream:&lt;url&gt;, electron:&lt;url&gt; and a
/// web page URL.
/// </summary>
public sealed class ScreenAssignments
{
    private readonly ConcurrentDictionary<uint, string> _byMap = new();

    public void Set(uint mapId, string source) => _byMap[mapId] = Normalize(source);

    /// <summary>An empty main area.</summary>
    public const string Blank = "blank";

    /// <summary>The diagnostic page itself, whatever the map is set to.</summary>
    public const string TestScreen = "testscreen";

    /// <summary>A plain title card, "aisp-emu" on a dark background: the default look for a display.</summary>
    public const string TitleCard = "title";

    /// <summary>The diagnostic page with a fine coordinate grid, for measuring screen panels.</summary>
    public const string Calibrate = "calibrate";

    /// <summary>
    /// The launcher hook's own calibration picture and test tone, generated in the client at the
    /// panel's exact size. pattern:live counts from zero whenever it starts, like a stream. Bare
    /// "pattern" is the same thing.
    /// </summary>
    public const string PatternLive = "pattern:live";

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
    /// forms (tw: to twitch:, a bare lv… id to nico:, bare pattern to pattern:live).
    /// </summary>
    public static string Normalize(string source)
    {
        var words = source.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return "";
        var main = words[0];
        if (main.StartsWith("tw:", StringComparison.OrdinalIgnoreCase))
            main = "twitch:" + main[3..];
        else if (IsNicoLiveId(main))
            main = "nico:" + main;
        else if (string.Equals(main, "pattern", StringComparison.OrdinalIgnoreCase))
            main = PatternLive;
        else if (string.Equals(main, PatternLive, StringComparison.OrdinalIgnoreCase))
            main = main.ToLowerInvariant();
        return string.Join(' ', new[] { main }.Concat(words.Skip(1)));
    }

    private static string MainOf(string source) => Normalize(source).Split(' ')[0];

    private static IEnumerable<string> ExtrasOf(string source) =>
        Normalize(source).Split(' ').Skip(1);

    public bool Clear(uint mapId) => _byMap.TryRemove(mapId, out _);

    public string? Get(uint mapId) => _byMap.TryGetValue(mapId, out var source) ? source : null;

    // The ids end up inside URLs on every viewer's command line (streamlink, the browser host),
    // so only the characters the sites themselves use are let through.

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

    /// <summary>ytl:&lt;id&gt;: a YouTube live stream through streamlink.</summary>
    public static bool IsYouTubeLiveSource(string? source) =>
        source is not null && HasPrefix(MainOf(source), "ytl:", IsYouTubeId);

    /// <summary>pattern:live, the hook's own test picture and tone.</summary>
    public static bool IsPatternSource(string? source) =>
        source is not null
        && string.Equals(MainOf(source), PatternLive, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// electron:&lt;http(s) url&gt;: any page in the off-screen browser, overlaid like a stream.
    /// Only from /screen; a typed room TV id would make every viewer's client fetch the page.
    /// </summary>
    public static bool IsElectronSource(string? source) =>
        source is not null && HasPrefix(MainOf(source), "electron:", IsPageUrl);

    /// <summary>Sources the off-screen browser shows: electron:&lt;url&gt; and the Twitch embed.</summary>
    public static bool IsBrowserSource(string? source) =>
        IsElectronSource(source) || IsTwitchEmbedSource(source);

    /// <summary>
    /// The ids anyone may type into a room TV: short ids that expand to a known site (or the
    /// test pattern), never an arbitrary URL, since every viewer's client fetches what is typed.
    /// </summary>
    public static bool IsTypedSource(string? source) =>
        IsTwitchSource(source)
        || IsTwitchEmbedSource(source)
        || IsNicoLiveSource(source)
        || IsYouTubeLiveSource(source)
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

    /// <summary>A web page the screen shows as is, framed inside the screen page.</summary>
    public static bool IsPageUrl(string? source) =>
        source is not null
        && (
            MainOf(source).StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || MainOf(source).StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        );

    /// <summary>
    /// What /screen accepts: a stream, a page, blank, the title card, the test page, the
    /// calibration aids. A single word: nothing may follow it.
    /// </summary>
    public static bool IsValidSource(string? source)
    {
        if (source is null)
            return false;
        var main = MainOf(source);
        if (ExtrasOf(source).Any())
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
    /// opens), stream:&lt;url&gt; (anything ffmpeg opens), pattern:live and
    /// electron:&lt;http(s) url&gt;; the friendly ids are vocabulary of this server. Page URLs
    /// and the page's own keywords pass through.
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
        else if (IsNicoLiveSource(main))
            main = "streamlink:https://live.nicovideo.jp/watch/" + main[5..];
        else if (IsYouTubeLiveSource(main))
            main = "streamlink:https://www.youtube.com/watch?v=" + main[4..];
        return string.Join(' ', new[] { main }.Concat(words.Skip(1)));
    }

    /// <summary>
    /// The source a screen page should publish, given the route the hook sent the client to,
    /// the movie id (room TVs only) and the map the hook read from the client. Null means the
    /// page shows its diagnostics. A typed movie id only reaches this for the typed ids
    /// (<see cref="IsTypedSource"/>), the title card, the test page and the calibration grid:
    /// anything else typed (the c: rectangles included) falls back to the map's assignment. An
    /// unassigned room TV stays blank; an unassigned town screen shows the title card.
    /// </summary>
    public string? Resolve(string route, string? movieId, uint? mapId)
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
            if (IsTypedSource(typed))
                return ToHookSource(typed);
        }
        var source = mapId is { } map && _byMap.TryGetValue(map, out var found) ? found : null;
        if (source is null)
            return route == "room-tv" ? Blank : TitleCard;
        if (string.Equals(source, TestScreen, StringComparison.OrdinalIgnoreCase))
            return null;
        return ToHookSource(source);
    }
}
