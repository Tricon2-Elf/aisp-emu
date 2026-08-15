using aisp.Common.Localisation;

namespace aisp.Common.DAL.Entities;

public sealed class LocalisedText
{
    public string Key { get; set; } = string.Empty;
    public GameLanguage Language { get; set; }
    public string Value { get; set; } = string.Empty;
}
