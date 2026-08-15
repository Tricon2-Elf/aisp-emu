using System.Globalization;

namespace aisp.Common.Localisation;

public enum GameLanguage : byte
{
    Japanese = 0,
    English = 1,
    ChineseSimplified = 2,
    ChineseTraditional = 3,
}

public static class GameLanguages
{
    private static readonly Entry[] Entries =
    [
        new(GameLanguage.Japanese, "ja", "ja-JP", ["ja", "ja-JP", "jp"]),
        new(GameLanguage.English, "en", "en-US", ["en", "en-US", "en-GB"]),
        new(
            GameLanguage.ChineseSimplified,
            "zh-Hans",
            "zh-CN",
            ["zh-Hans", "zh-CN", "zh-Hans-CN", "zh"]
        ),
        new(
            GameLanguage.ChineseTraditional,
            "zh-Hant",
            "zh-TW",
            ["zh-Hant", "zh-TW", "zh-HK", "zh-Hant-TW"]
        ),
    ];

    private static readonly Dictionary<GameLanguage, Entry> ByLanguage = Entries.ToDictionary(
        entry => entry.Language
    );

    public static IReadOnlyList<GameLanguage> All { get; } =
        Entries.Select(entry => entry.Language).ToArray();

    public static string ToTag(this GameLanguage language) =>
        ByLanguage.TryGetValue(language, out var entry) ? entry.Tag : Entries[0].Tag;

    public static CultureInfo GetCulture(this GameLanguage language) =>
        CultureInfo.GetCultureInfo(
            ByLanguage.TryGetValue(language, out var entry) ? entry.Culture : Entries[0].Culture
        );

    public static bool TryParse(string? value, out GameLanguage language)
    {
        language = GameLanguage.Japanese;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var tag = value.Trim();
        foreach (var entry in Entries)
        {
            foreach (var alias in entry.Aliases)
            {
                if (alias == tag)
                {
                    language = entry.Language;
                    return true;
                }
            }
        }

        return false;
    }

    public static GameLanguage ParseOrDefault(
        string? value,
        GameLanguage fallback = GameLanguage.Japanese
    ) => TryParse(value, out var language) ? language : fallback;

    private readonly record struct Entry(
        GameLanguage Language,
        string Tag,
        string Culture,
        string[] Aliases
    );
}
