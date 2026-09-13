using aisp.Common.Localisation;

namespace aisp.Common.Config;

public class MotdOptions
{
    public const string SectionName = "Motd";

    /// <summary>When false, no MOTD is sent even if <see cref="Message"/> or <see cref="Messages"/> is set.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Fallback MOTD shown in the client's System / Notice chat box on Area Server connect.
    /// Used when no language-specific entry exists. Empty means no MOTD (unless <see cref="Messages"/> has a match).
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// Optional per-locale MOTD keyed by language tag (<c>ja</c>, <c>en</c>, <c>zh-Hans</c>, <c>zh-Hant</c>).
    /// </summary>
    public Dictionary<string, string> Messages { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public bool TryGetMessage(GameLanguage language, out string message)
    {
        message = "";
        if (!Enabled)
            return false;

        if (TryGetLocalised(language, out message))
            return true;

        if (!string.IsNullOrWhiteSpace(Message))
        {
            message = Message.Trim();
            return true;
        }

        return false;
    }

    private bool TryGetLocalised(GameLanguage language, out string message)
    {
        message = "";
        if (Messages.Count == 0)
            return false;

        if (
            Messages.TryGetValue(language.ToTag(), out var exact)
            && !string.IsNullOrWhiteSpace(exact)
        )
        {
            message = exact.Trim();
            return true;
        }

        foreach (var (key, value) in Messages)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;
            if (GameLanguages.TryParse(key, out var parsed) && parsed == language)
            {
                message = value.Trim();
                return true;
            }
        }

        return false;
    }
}
