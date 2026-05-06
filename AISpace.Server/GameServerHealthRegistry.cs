using System.Collections.Concurrent;
using AISpace.Common;
using AISpace.Common.DAL.Repositories;

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

    public GameServerHealthRegistry()
    {
    }

    public void AddServer(string key, int port)
    {
        _info.TryAdd(key, new ServerHealthInfo(KeyToDisplayName(key), port, "starting"));
    }

    public void MarkListening(string key, int port)
    {
        _info.AddOrUpdate(key, _ => new ServerHealthInfo(KeyToDisplayName(key), port, "healthy"), (_, existing) => existing with { Port = port, State = "healthy" });
    }

    public IReadOnlyDictionary<string, ServerHealthInfo> GetSnapshot(ISessionPresenceRepository presenceRepo)
    {
        return _info.ToDictionary(
            kv => kv.Key,
            kv =>
            {
                var usernames = ConnectedUsernamesForKey(kv.Key, presenceRepo);
                return kv.Value with { ConnectedClients = usernames.Count, ConnectedUsernames = usernames };
            }
        );
    }

    private static IReadOnlyList<string> ConnectedUsernamesForKey(string key, ISessionPresenceRepository repo)
    {
        var serverType = key switch
        {
            Keys.AuthServer => ServerType.Auth,
            Keys.MsgServer => ServerType.Msg,
            Keys.AreaServer => ServerType.Area,
            _ => (ServerType?)null,
        };

        if (serverType == null)
            return [];

        var presences = repo.GetByServerType(serverType.Value);
        return presences.Select(p => p.UserId).Distinct().Select(uid => uid.ToString()).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
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
