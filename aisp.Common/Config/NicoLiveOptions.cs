namespace aisp.Common.Config;

public sealed class NicoLiveOptions
{
    /// <summary>
    /// Live-program ID sent to maps containing Nico Live billboard surfaces. An empty value disables playback.
    /// "lv1" is a dummy placeholder, not a real programme; set to an actual lv&lt;digits&gt; id to enable it.
    /// </summary>
    public string LiveId { get; set; } = "lv1";
}
