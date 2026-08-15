using aisp.Common.DAL;
using aisp.Common.Game;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Localisation;

public sealed class TextLocaliser(IServiceScopeFactory scopeFactory, ILogger<TextLocaliser> logger)
    : ITextLocaliser
{
    private Dictionary<(string Key, GameLanguage Language), string> _values = [];

    public string Get(IPlayerSession session, LocKey key, params object[] args) =>
        Get(session.Language, key, args);

    public string Get(GameLanguage language, LocKey key, params object[] args)
    {
        var value = Resolve(language, key);
        if (args.Length == 0)
            return value;

        try
        {
            return string.Format(language.GetCulture(), value, args);
        }
        catch (FormatException)
        {
            logger.LogWarning(
                "Localisation key {Key} has invalid format placeholders for {Language}",
                key.Value,
                language
            );
            return value;
        }
    }

    public async Task ReloadAsync(CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainContext>();
        var rows = await db.LocalisedTexts.AsNoTracking().ToListAsync(ct);
        var values = new Dictionary<(string Key, GameLanguage Language), string>(rows.Count);
        foreach (var row in rows)
        {
            values[(row.Key, row.Language)] = row.Value;
        }

        _values = values;
    }

    private string Resolve(GameLanguage language, LocKey key)
    {
        if (_values.TryGetValue((key.Value, language), out var requested))
            return requested;

        if (
            language != GameLanguage.Japanese
            && _values.TryGetValue((key.Value, GameLanguage.Japanese), out var japanese)
        )
        {
            return japanese;
        }
        return key.Value;
    }
}
