using System.Text.Json;
using System.Text.RegularExpressions;

namespace aisp.Common.Localisation;

public static class LocalisationSeedValidator
{
    private static readonly Regex PlaceholderPattern = new(@"\{(\d+)\}", RegexOptions.Compiled);

    public static IReadOnlyList<string> Validate(
        IReadOnlyList<(string Key, GameLanguage Language, string Value)> rows
    )
    {
        var warnings = new List<string>();
        var seen = new HashSet<(string Key, GameLanguage Language)>();
        foreach (var row in rows)
        {
            if (!seen.Add((row.Key, row.Language)))
                warnings.Add($"Duplicate Localisation key '{row.Key}' for {row.Language}.");
        }

        foreach (var group in rows.GroupBy(row => row.Key, StringComparer.Ordinal))
        {
            var japanese = group.FirstOrDefault(row => row.Language == GameLanguage.Japanese);
            if (string.IsNullOrEmpty(japanese.Key))
            {
                warnings.Add($"Localisation key '{group.Key}' is missing Japanese.");
                continue;
            }

            var expected = Placeholders(japanese.Value);
            foreach (var row in group)
            {
                if (row.Language == GameLanguage.Japanese)
                    continue;
                if (!Placeholders(row.Value).SetEquals(expected))
                    warnings.Add(
                        $"Localisation key '{row.Key}' placeholders for {row.Language} do not match Japanese."
                    );
            }
        }

        return warnings;
    }

    public static IReadOnlyList<string> UnsupportedLocaleTags(JsonElement element, string path)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return [];

        var warnings = new List<string>();
        foreach (var property in element.EnumerateObject())
        {
            if (!GameLanguages.TryParse(property.Name, out _))
                warnings.Add($"Unsupported locale tag '{property.Name}' at {path}.");
        }

        return warnings;
    }

    private static HashSet<int> Placeholders(string value) =>
        PlaceholderPattern
            .Matches(value)
            .Select(match => int.Parse(match.Groups[1].Value))
            .ToHashSet();
}
