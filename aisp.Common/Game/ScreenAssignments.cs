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
    /// forms (tw: to twitch:, a bare lv… id to nico:, bare pattern to pattern:live). A source is
    /// "&lt;main&gt; [main:&lt;url&gt;] [banner:&lt;url&gt;] [&lt;url&gt;] [box:x/y/w/h]
    /// [crop:sw/sh:cx/cy] [scrollx:N] [scrolly:N] [scroll:x/y] [scale:N] [key[:RRGGBB]]":
    /// main:&lt;url&gt; is a frame page under the main panel, with the box relative to that
    /// panel; banner:&lt;url&gt; is a page for the Stage banner strip (the title card when
    /// absent); a bare page URL is the raw form, a frame page under the whole crop with a box or
    /// key word, the banner otherwise; a box word places the video inside the crop; a crop word
    /// renders the picture at another size and shows the box-sized window at cx,cy of it (for
    /// browser sources sw/sh is the layout viewport); scroll words pan a browser document;
    /// scale:N is browser zoom; a key word colour-keys the video into the page.
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
    /// scroll and scale words).
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
