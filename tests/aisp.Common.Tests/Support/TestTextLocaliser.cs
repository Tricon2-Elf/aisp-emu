using aisp.Common.Game;
using aisp.Common.Localisation;

namespace aisp.Common.Tests.Support;

internal sealed class TestTextLocaliser : ITextLocaliser
{
    public static readonly TestTextLocaliser English = FromSeed();

    private readonly Dictionary<(string Key, GameLanguage Language), string> _values;

    public TestTextLocaliser(Dictionary<(string Key, GameLanguage Language), string> values)
    {
        _values = values;
    }

    public static TestTextLocaliser FromSeed()
    {
        var values = new Dictionary<(string Key, GameLanguage Language), string>();
        var seedDirectory = FindSeedDirectory();
        if (seedDirectory is not null)
        {
            foreach (
                var (key, language, text) in LocalisationCatalogSeeder.CollectFromDirectory(
                    seedDirectory
                )
            )
                values[(key, language)] = text;
        }

        return new TestTextLocaliser(values);
    }

    public string Get(GameLanguage language, LocKey key, params object[] args)
    {
        TryGet(language, key, out var value);
        return Format(value, language, args);
    }

    public string Get(IPlayerSession session, LocKey key, params object[] args) =>
        Get(session.Language, key, args);

    public bool TryGet(GameLanguage language, LocKey key, out string value)
    {
        if (_values.TryGetValue((key.Value, language), out var requested))
        {
            value = requested;
            return true;
        }
        if (
            language != GameLanguage.Japanese
            && _values.TryGetValue((key.Value, GameLanguage.Japanese), out var japanese)
        )
        {
            value = japanese;
            return true;
        }
        value = key.Value;
        return false;
    }

    public Task ReloadAsync(CancellationToken ct = default) => Task.CompletedTask;

    public IReadOnlyList<string> ReportMissing(GameLanguage language) => [];

    private static string Format(string value, GameLanguage language, object[] args)
    {
        if (args.Length == 0)
            return value;
        return string.Format(language.GetCulture(), value, args);
    }

    private static string? FindSeedDirectory()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "seedData"),
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "aisp.Common",
                "seedData"
            ),
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    "aisp.Common",
                    "seedData"
                )
            ),
        };
        return candidates.FirstOrDefault(Directory.Exists);
    }
}
