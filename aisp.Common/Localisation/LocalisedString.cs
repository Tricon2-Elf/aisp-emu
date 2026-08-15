using System.Text.Json;
using System.Text.Json.Serialization;

namespace aisp.Common.Localisation;

[JsonConverter(typeof(LocalisedStringJsonConverter))]
public sealed class LocalisedString
{
    private readonly Dictionary<GameLanguage, string?> _values = [];

    public string? this[GameLanguage language]
    {
        get => _values.GetValueOrDefault(language);
        set => _values[language] = value;
    }

    public string? Ja
    {
        get => this[GameLanguage.Japanese];
        set => this[GameLanguage.Japanese] = value;
    }

    public string? En
    {
        get => this[GameLanguage.English];
        set => this[GameLanguage.English] = value;
    }

    public string? ZhHans
    {
        get => this[GameLanguage.ChineseSimplified];
        set => this[GameLanguage.ChineseSimplified] = value;
    }

    public string? ZhHant
    {
        get => this[GameLanguage.ChineseTraditional];
        set => this[GameLanguage.ChineseTraditional] = value;
    }

    public bool IsEmpty => string.IsNullOrWhiteSpace(Canonical);

    public string Canonical =>
        GameLanguages
            .All.Select(language => this[language])
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
        ?? string.Empty;

    public static LocalisedString FromCanonical(string value) => new() { Ja = value };

    public string? Get(GameLanguage language) => this[language];

    public IEnumerable<(GameLanguage Language, string Value)> Enumerate()
    {
        foreach (var language in GameLanguages.All)
        {
            if (!_values.TryGetValue(language, out var value) || value is null)
                continue;
            if (language != GameLanguage.Japanese && string.IsNullOrWhiteSpace(value))
                continue;
            yield return (language, value);
        }
    }
}

public sealed class LocalisedStringJsonConverter : JsonConverter<LocalisedString>
{
    public override LocalisedString Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Null)
            return new LocalisedString();
        if (reader.TokenType == JsonTokenType.String)
            return LocalisedString.FromCanonical(reader.GetString() ?? string.Empty);
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException(
                $"Expected string or object for LocalisedString, got {reader.TokenType}."
            );

        var result = new LocalisedString();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return result;
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected property name in LocalisedString object.");

            var property = reader.GetString();
            reader.Read();
            var value = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
            if (GameLanguages.TryParse(property, out var language))
                result[language] = value;
        }

        throw new JsonException("Unterminated LocalisedString object.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        LocalisedString value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartObject();
        foreach (var (language, text) in value.Enumerate())
        {
            if (string.IsNullOrWhiteSpace(text))
                continue;
            writer.WriteString(language.ToTag(), text);
        }
        writer.WriteEndObject();
    }
}
