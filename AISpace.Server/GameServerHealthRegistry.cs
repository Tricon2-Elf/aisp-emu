using System.Collections.Concurrent;
using AISpace.Common;
using AISpace.Common.Config;
using Microsoft.Extensions.Options;

namespace AISpace.Server;

public sealed class GameServerHealthRegistry
{
    private readonly TimeSpan _heartbeatInterval;
    private readonly TimeSpan _stalenessThreshold;
    private readonly ConcurrentDictionary<ServerType, ServerHealthEntry> _entries = new();

    public GameServerHealthRegistry(IOptions<ServerOptions> options)
    {
        var health = options.Value.HealthCheck;
        _heartbeatInterval = TimeSpan.FromSeconds(Math.Max(1, health.HeartbeatIntervalSeconds));
        _stalenessThreshold = TimeSpan.FromSeconds(Math.Max(1, health.StalenessSeconds));
    }

    public TimeSpan HeartbeatInterval => _heartbeatInterval;

    public TimeSpan StalenessThreshold => _stalenessThreshold;

    public void AddServer(ServerType serverType, int port)
    {
        _entries.TryAdd(
            serverType,
            new ServerHealthEntry(ToDisplayName(serverType), port, "starting", null, DateTime.UtcNow, null)
        );
    }

    public void MarkListening(ServerType serverType, int port)
    {
        var now = DateTime.UtcNow;
        _entries.AddOrUpdate(
            serverType,
            _ => new ServerHealthEntry(ToDisplayName(serverType), port, "healthy", null, now, now),
            (_, existing) =>
                existing with
                {
                    Port = port,
                    ReportedState = "healthy",
                    LastError = null,
                    LastHeartbeatUtc = now,
                }
        );
    }

    public void MarkUnhealthy(ServerType serverType, string reason)
    {
        _entries.AddOrUpdate(
            serverType,
            _ => new ServerHealthEntry(ToDisplayName(serverType), 0, "unhealthy", reason, DateTime.UtcNow, null),
            (_, existing) => existing with { ReportedState = "unhealthy", LastError = reason }
        );
    }

    public void RecordHeartbeat(ServerType serverType)
    {
        var now = DateTime.UtcNow;
        _entries.AddOrUpdate(
            serverType,
            _ => new ServerHealthEntry(ToDisplayName(serverType), 0, "healthy", null, now, now),
            (_, existing) => existing with { LastHeartbeatUtc = now }
        );
    }

    public void SetAcceptCheck(ServerType serverType, Func<bool> isAccepting)
    {
        if (_entries.TryGetValue(serverType, out var entry))
            _entries[serverType] = entry with { AcceptCheck = isAccepting };
    }

    public void ClearAcceptCheck(ServerType serverType)
    {
        if (_entries.TryGetValue(serverType, out var entry))
            _entries[serverType] = entry with { AcceptCheck = null };
    }

    public IReadOnlyDictionary<string, ServerHealthInfo> GetSnapshot()
    {
        var now = DateTime.UtcNow;
        return _entries.ToDictionary(kv => ToJsonKey(kv.Key), kv => Evaluate(kv.Value, now));
    }

    private ServerHealthInfo Evaluate(ServerHealthEntry entry, DateTime now)
    {
        var listenerAccepting = entry.AcceptCheck?.Invoke();

        if (entry.ReportedState == "unhealthy")
        {
            return ToInfo(
                entry,
                "unhealthy",
                entry.LastError,
                listenerAccepting
            );
        }

        if (entry.ReportedState == "healthy" && listenerAccepting == false)
        {
            return ToInfo(
                entry,
                "unhealthy",
                "listener not accepting",
                listenerAccepting
            );
        }

        if (entry.ReportedState == "healthy")
        {
            if (entry.LastHeartbeatUtc is null || now - entry.LastHeartbeatUtc.Value > _stalenessThreshold)
            {
                return ToInfo(
                    entry,
                    "unhealthy",
                    $"heartbeat stale (>{_stalenessThreshold.TotalSeconds:0}s)",
                    listenerAccepting
                );
            }
        }
        else if (entry.ReportedState == "starting" && now - entry.RegisteredAtUtc > _stalenessThreshold)
        {
            return ToInfo(
                entry,
                "unhealthy",
                "timed out while starting",
                listenerAccepting
            );
        }

        return ToInfo(entry, entry.ReportedState, entry.LastError, listenerAccepting);
    }

    private static ServerHealthInfo ToInfo(ServerHealthEntry entry, string state, string? lastError, bool? listenerAccepting) =>
        new(entry.Name, entry.Port, state, lastError, entry.LastHeartbeatUtc, listenerAccepting);

    private static string ToDisplayName(ServerType serverType) => $"{serverType}Server";

    private static string ToJsonKey(ServerType serverType) => char.ToLowerInvariant(serverType.ToString()[0]) + serverType.ToString()[1..] + "Server";

    private sealed record ServerHealthEntry(
        string Name,
        int Port,
        string ReportedState,
        string? LastError,
        DateTime RegisteredAtUtc,
        DateTime? LastHeartbeatUtc,
        Func<bool>? AcceptCheck = null
    );
}

public sealed record ServerHealthInfo(string Name, int Port, string State, string? LastError, DateTime? LastHeartbeatUtc, bool? ListenerAccepting);
