using System.Text.Json;

namespace aisp.Common.Localisation;

public static class SeedJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new LocalisedStringJsonConverter() },
    };
}
