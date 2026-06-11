using System.Collections.Concurrent;
using System.Net.Sockets;
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
        _info.AddOrUpdate(serverType, _ => new ServerHealthInfo(ToDisplayName(serverType), port, "healthy", null), (_, existing) => existing with { Port = port, State = "healthy", LastError = null });
    }

    public void MarkUnhealthy(ServerType serverType, string reason)
    {
        _info.AddOrUpdate(serverType, _ => new ServerHealthInfo(ToDisplayName(serverType), 0, "unhealthy", reason), (_, existing) => existing with { State = "unhealthy", LastError = reason });
    }

    public async Task<bool> ProbeTcpPortAsync(int port, CancellationToken ct = default)
    {
        try
        {
            using var client = new TcpClient();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(2));
            await client.ConnectAsync("127.0.0.1", port, timeout.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IReadOnlyDictionary<string, ServerHealthInfo>> GetVerifiedSnapshotAsync(CancellationToken ct = default)
    {
        var snapshot = _info.ToArray();
        var verified = new Dictionary<string, ServerHealthInfo>(snapshot.Length);
        foreach (var (serverType, info) in snapshot)
        {
            var reachable = info.State == "healthy" && await ProbeTcpPortAsync(info.Port, ct);
            verified[ToJsonKey(serverType)] = reachable ? info : info with { State = "unhealthy", LastError = info.LastError ?? "tcp probe failed" };
        }

        return verified;
    }

    public IReadOnlyDictionary<string, ServerHealthInfo> GetSnapshot() => _info.ToDictionary(kv => ToJsonKey(kv.Key), kv => kv.Value);

    private static string ToDisplayName(ServerType serverType) => $"{serverType}Server";

    private static string ToJsonKey(ServerType serverType) => char.ToLowerInvariant(serverType.ToString()[0]) + serverType.ToString()[1..] + "Server";
}

public sealed record ServerHealthInfo(string Name, int Port, string State, string? LastError);
