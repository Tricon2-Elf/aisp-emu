using System.Collections.Concurrent;
using AISpace.Common;

namespace AISpace.Server;

public sealed class GameServerHealthRegistry
{
    private readonly ConcurrentDictionary<ServerType, ServerHealthInfo> _info = new();

    public void AddServer(ServerType serverType, int port)
    {
        _info.TryAdd(serverType, new ServerHealthInfo(ToDisplayName(serverType), port, "starting", null));
    }

    public void MarkListening(ServerType serverType, int port)
    {
        _info.AddOrUpdate(
            serverType,
            _ => new ServerHealthInfo(ToDisplayName(serverType), port, "healthy", null),
            (_, existing) => existing with
            {
                Port = port,
                State = "healthy",
                LastError = null,
            }
        );
    }

    public void MarkUnhealthy(ServerType serverType, string reason)
    {
        _info.AddOrUpdate(
            serverType,
            _ => new ServerHealthInfo(ToDisplayName(serverType), 0, "unhealthy", reason),
            (_, existing) => existing with { State = "unhealthy", LastError = reason }
        );
    }

    public IReadOnlyDictionary<string, ServerHealthInfo> GetSnapshot() => _info.ToDictionary(kv => ToJsonKey(kv.Key), kv => kv.Value);

    private static string ToDisplayName(ServerType serverType) => $"{serverType}Server";

    private static string ToJsonKey(ServerType serverType) => char.ToLowerInvariant(serverType.ToString()[0]) + serverType.ToString()[1..] + "Server";
}

public sealed record ServerHealthInfo(string Name, int Port, string State, string? LastError);
