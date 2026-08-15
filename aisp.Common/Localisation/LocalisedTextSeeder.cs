using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.Localisation;

public static class LocalisedTextSeeder
{
    public static async Task UpsertMissingAsync(
        MainContext db,
        IEnumerable<(string Key, GameLanguage Language, string Value)> rows,
        CancellationToken ct = default
    )
    {
        var incoming = rows.Where(row => !string.IsNullOrWhiteSpace(row.Key))
            .DistinctBy(row => (row.Key, row.Language))
            .ToList();
        if (incoming.Count == 0)
            return;

        var existing = (
            await db
                .LocalisedTexts.AsNoTracking()
                .Select(row => new { row.Key, row.Language })
                .ToListAsync(ct)
        )
            .Select(row => (row.Key, row.Language))
            .ToHashSet();

        var missing = incoming
            .Where(row => existing.Add((row.Key, row.Language)))
            .Select(row => new LocalisedText
            {
                Key = row.Key,
                Language = row.Language,
                Value = row.Value,
            })
            .ToList();
        if (missing.Count == 0)
            return;

        db.LocalisedTexts.AddRange(missing);
        await db.SaveChangesAsync(ct);
    }

    public static IEnumerable<(string Key, GameLanguage Language, string Value)> FromLocalised(
        string key,
        LocalisedString? value
    )
    {
        if (value is null)
            yield break;
        foreach (var (language, text) in value.Enumerate())
            yield return (key, language, text);
    }

    public static LocalisedString Read(System.Text.Json.JsonElement element)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.String)
            return LocalisedString.FromCanonical(element.GetString() ?? string.Empty);
        if (element.ValueKind != System.Text.Json.JsonValueKind.Object)
            return new LocalisedString();

        var result = new LocalisedString();
        foreach (var language in GameLanguages.All)
            result[language] = ReadProperty(element, language.ToTag());
        return result;
    }

    private static string? ReadProperty(System.Text.Json.JsonElement element, string name) =>
        element.TryGetProperty(name, out var property)
        && property.ValueKind == System.Text.Json.JsonValueKind.String
            ? property.GetString()
            : null;
}
