using aisp.Common.Game;

namespace aisp.Common.Localisation;

public static class TextLocaliserExtensions
{
    public static string GetOr(
        this ITextLocaliser localiser,
        GameLanguage language,
        LocKey key,
        LocKey fallback
    ) => localiser.TryGet(language, key, out var value) ? value : localiser.Get(language, fallback);
}
