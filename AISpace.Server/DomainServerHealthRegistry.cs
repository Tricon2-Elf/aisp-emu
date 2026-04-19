using System.Collections.Concurrent;
using AISpace.Common.Game;

namespace AISpace.Server;

public sealed class DomainServerHealthRegistry
{
    public static class Keys
    {
        public const string AuthServer = "authServer";
        public const string MsgServer = "msgServer";
        public const string AreaServer = "areaServer";
    }

    private readonly ConcurrentDictionary<string, ServerHealthInfo> _info = new();

    public DomainServerHealthRegistry()
    {
        _info[Keys.AuthServer] = new ServerHealthInfo("AuthServer", 50050, "starting");
        _info[Keys.MsgServer] = new ServerHealthInfo("MsgServer", 50052, "starting");
        _info[Keys.AreaServer] = new ServerHealthInfo("AreaServer", 50054, "starting");
    }

    public void MarkListening(string key, int port)
    {
        _info.AddOrUpdate(key, _ => new ServerHealthInfo(KeyToDisplayName(key), port, "healthy"), (_, existing) => existing with { Port = port, State = "healthy" });
    }

    public IReadOnlyDictionary<string, ServerHealthInfo> GetSnapshot(SharedState state)
    {
        return _info.ToDictionary(
            kv => kv.Key,
            kv =>
            {
                var usernames = ConnectedUsernamesForKey(kv.Key, state);
                return kv.Value with { ConnectedClients = usernames.Count, ConnectedUsernames = usernames };
            }
        );
    }

    private static IReadOnlyList<string> ConnectedUsernamesForKey(string key, SharedState state)
    {
        var clients = key switch
        {
            Keys.AuthServer => state.AuthClients,
            Keys.MsgServer => state.MsgClients,
            Keys.AreaServer => state.AreaClients,
            _ => null,
        };

        if (clients is null)
            return Array.Empty<string>();

        return clients.Values.Select(session => session.User?.Username).Where(name => !string.IsNullOrWhiteSpace(name)).Select(name => name!).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string KeyToDisplayName(string key) =>
        key switch
        {
            Keys.AuthServer => "AuthServer",
            Keys.MsgServer => "MsgServer",
            Keys.AreaServer => "AreaServer",
            _ => key,
        };
}

public sealed record ServerHealthInfo(string Name, int Port, string State, int ConnectedClients = 0, IReadOnlyList<string>? ConnectedUsernames = null);
