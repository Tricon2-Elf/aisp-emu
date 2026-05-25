using System.Collections.Concurrent;
using AISpace.Common;

namespace AISpace.Server;

public sealed class GameServerHealthRegistry
{
    private readonly ConcurrentDictionary<ServerType, ServerHealthInfo> _info = new();

    public void AddServer(ServerType serverType, int port)
    {
        _info.TryAdd(serverType, new ServerHealthInfo(ToDisplayName(serverType), port, "starting"));
    }

    public void MarkListening(ServerType serverType, int port)
    {
        _info.AddOrUpdate(serverType, _ => new ServerHealthInfo(ToDisplayName(serverType), port, "healthy"), (_, existing) => existing with { Port = port, State = "healthy" });
    }

    public IReadOnlyDictionary<string, ServerHealthInfo> GetSnapshot() => _info.ToDictionary(kv => ToJsonKey(kv.Key), kv => kv.Value);

    private static string ToDisplayName(ServerType serverType) => $"{serverType}Server";

    private static string ToJsonKey(ServerType serverType) => char.ToLowerInvariant(serverType.ToString()[0]) + serverType.ToString()[1..] + "Server";
}

public sealed record ServerHealthInfo(string Name, int Port, string State);
