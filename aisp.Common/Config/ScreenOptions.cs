namespace aisp.Common.Config;

/// <summary>How the short screen ids that leave the choice to the server play (ScreenAssignments).</summary>
public sealed class ScreenOptions
{
    /// <summary>tw:&lt;channel&gt;: "electron" (Twitch's own player in the off-screen browser, as twe:)
    /// or "streamlink" (decoded through streamlink and ffmpeg, as twl:).</summary>
    public string Twitch { get; set; } = "electron";

    /// <summary>yt:&lt;id&gt;: "electron" (YouTube's own player in the off-screen browser, as yte:)
    /// or "yt-dlp" (resolved by yt-dlp and decoded by ffmpeg, as ytd:).</summary>
    public string YouTube { get; set; } = "electron";

    /// <summary>sm… (nn:sm…): "electron" (Nico's own player embed in the off-screen browser, as nne:)
    /// or "yt-dlp" (resolved by yt-dlp and decoded by ffmpeg, as nnd:).</summary>
    public string NicoVideo { get; set; } = "electron";

    /// <summary>lv… (nn:lv…): "electron" (the Nico Live watch page in the off-screen browser with its
    /// player scaled to the box, as nne:) or "streamlink" (decoded through streamlink and ffmpeg, as nnl:).</summary>
    public string NicoLive { get; set; } = "electron";
}
