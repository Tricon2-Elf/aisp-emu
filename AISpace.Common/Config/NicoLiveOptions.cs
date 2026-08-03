namespace AISpace.Common.Config;

public sealed class NicoLiveOptions
{
    /// <summary>
    /// Live-program ID sent to maps containing Nico Live billboard surfaces. An empty value disables playback.
    /// </summary>
    public string LiveId { get; set; } = "lv1";
}
