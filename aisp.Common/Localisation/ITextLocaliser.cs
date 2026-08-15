using aisp.Common.Game;

namespace aisp.Common.Localisation;

public interface ITextLocaliser
{
    string Get(GameLanguage language, LocKey key, params object[] args);

    string Get(IPlayerSession session, LocKey key, params object[] args);

    Task ReloadAsync(CancellationToken ct = default);
}
