using System.Collections.Concurrent;

namespace AISpace.Server;

public sealed class GameServerHealthRegistry
{
    public static class Keys
    {
        public const string AuthServer = "authServer";
        public const string MsgServer = "msgServer";
        public const string AreaServer = "areaServer";
    }

    private readonly ConcurrentDictionary<string, ServerHealthInfo> _info = new();

    public GameServerHealthRegistry() { }

    public void AddServer(string key, int port)
    {
        _info.TryAdd(key, new ServerHealthInfo(KeyToDisplayName(key), port, "starting"));
    }

    public void MarkListening(string key, int port)
    {
        _info.AddOrUpdate(key, _ => new ServerHealthInfo(KeyToDisplayName(key), port, "healthy"), (_, existing) => existing with { Port = port, State = "healthy" });
    }

    public IReadOnlyDictionary<string, ServerHealthInfo> GetSnapshot() => _info.ToDictionary(kv => kv.Key, kv => kv.Value);

    private static string KeyToDisplayName(string key) =>
        key switch
        {
            Keys.AuthServer => "AuthServer",
            Keys.MsgServer => "MsgServer",
            Keys.AreaServer => "AreaServer",
            _ => key,
        };
}

public sealed record ServerHealthInfo(string Name, int Port, string State);
