using System.Collections.Concurrent;
using System.Diagnostics;
using AISpace.Common;
using AISpace.Common.Config;
using AISpace.Network;
using Microsoft.Extensions.Options;

namespace AISpace.Server;

public sealed class GameServerHealthRegistry
{
    private readonly TimeSpan _heartbeatInterval;
    private readonly TimeSpan _stalenessThreshold;
    private readonly TimeSpan _schedulerTickInterval;
    private readonly TimeSpan _schedulerMaxLag;
    private readonly TimeSpan _schedulerMaxStale;
    private readonly int _idleMaxProcessCpuPercent;
    private readonly int _idleMaxActiveHandlers;
    private readonly ConcurrentDictionary<ServerType, ServerHealthEntry> _entries = new();

    private readonly object _processLock = new();
    private DateTime _lastSchedulerTickUtc;
    private TimeSpan _lastSchedulerLag = TimeSpan.Zero;
    private double _processCpuPercent;
    private TimeSpan _lastCpuSample;
    private DateTime _lastCpuSampleUtc;

    public GameServerHealthRegistry(IOptions<ServerOptions> options)
    {
        var health = options.Value.HealthCheck;
        _heartbeatInterval = TimeSpan.FromSeconds(Math.Max(1, health.HeartbeatIntervalSeconds));
        _stalenessThreshold = TimeSpan.FromSeconds(Math.Max(1, health.StalenessSeconds));
        _schedulerTickInterval = TimeSpan.FromSeconds(Math.Max(1, health.SchedulerTickSeconds));
        _schedulerMaxLag = TimeSpan.FromSeconds(Math.Max(1, health.SchedulerMaxLagSeconds));
        _schedulerMaxStale = TimeSpan.FromSeconds(Math.Max(1, health.SchedulerMaxStaleSeconds));
        _idleMaxProcessCpuPercent = Math.Max(0, health.IdleMaxProcessCpuPercent);
        _idleMaxActiveHandlers = Math.Max(0, health.IdleMaxActiveHandlers);
    }

    public TimeSpan HeartbeatInterval => _heartbeatInterval;

    public TimeSpan StalenessThreshold => _stalenessThreshold;

    public void AddServer(ServerType serverType, int port)
    {
        _entries.TryAdd(
            serverType,
            new ServerHealthEntry(
                ToDisplayName(serverType),
                port,
                "starting",
                null,
                DateTime.UtcNow,
                null
            )
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
            _ => new ServerHealthEntry(
                ToDisplayName(serverType),
                0,
                "unhealthy",
                reason,
                DateTime.UtcNow,
                null
            ),
            (_, existing) => existing with { ReportedState = "unhealthy", LastError = reason }
        );
    }

    public void RecordHeartbeat(ServerType serverType)
    {
        if (!_entries.TryGetValue(serverType, out var existing))
            return;

        _entries[serverType] = existing with { LastHeartbeatUtc = DateTime.UtcNow };
    }

    public bool IsRegistered(ServerType serverType) => _entries.ContainsKey(serverType);

    public void RecordSchedulerTick(TimeSpan lagSincePreviousTick)
    {
        lock (_processLock)
        {
            _lastSchedulerTickUtc = DateTime.UtcNow;
            _lastSchedulerLag = lagSincePreviousTick;
        }
    }

    public void SampleProcessCpu()
    {
        var proc = Process.GetCurrentProcess();
        var wallUtc = DateTime.UtcNow;
        var cpuTime = proc.TotalProcessorTime;

        lock (_processLock)
        {
            if (_lastCpuSampleUtc != default)
            {
                var wallMs = (wallUtc - _lastCpuSampleUtc).TotalMilliseconds;
                var cpuMs = (cpuTime - _lastCpuSample).TotalMilliseconds;
                if (wallMs >= 50)
                {
                    var cpuCount = Math.Max(1, Environment.ProcessorCount);
                    _processCpuPercent = cpuMs / wallMs / cpuCount * 100.0;
                }
            }

            _lastCpuSample = cpuTime;
            _lastCpuSampleUtc = wallUtc;
        }
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

    public void SetClientLoadCheck(ServerType serverType, Func<VceClientLoad> getClientLoad)
    {
        if (_entries.TryGetValue(serverType, out var entry))
            _entries[serverType] = entry with { ClientLoadCheck = getClientLoad };
    }

    public void ClearClientLoadCheck(ServerType serverType)
    {
        if (_entries.TryGetValue(serverType, out var entry))
            _entries[serverType] = entry with { ClientLoadCheck = null };
    }

    public HealthReport GetHealthReport()
    {
        var now = DateTime.UtcNow;
        var servers = _entries.ToDictionary(
            kv => ToJsonKey(kv.Key),
            kv => EvaluateServer(kv.Value, now)
        );
        var process = EvaluateProcess(servers, now);
        return new HealthReport(servers, process);
    }

    public IReadOnlyDictionary<string, ServerHealthInfo> GetSnapshot() => GetHealthReport().Servers;

    private ServerHealthInfo EvaluateServer(ServerHealthEntry entry, DateTime now)
    {
        var listenerAccepting = entry.AcceptCheck?.Invoke();
        var clientLoad = entry.ClientLoadCheck?.Invoke();

        if (entry.ReportedState == "unhealthy")
        {
            return ToInfo(entry, "unhealthy", entry.LastError, listenerAccepting, clientLoad);
        }

        if (entry.ReportedState == "healthy" && listenerAccepting == false)
        {
            return ToInfo(
                entry,
                "unhealthy",
                "listener not accepting",
                listenerAccepting,
                clientLoad
            );
        }

        if (
            entry.ReportedState == "healthy"
            && clientLoad is { MaxHandlers: > 0 } load
            && load.ActiveHandlers >= load.MaxHandlers
        )
        {
            return ToInfo(
                entry,
                "unhealthy",
                "client handler slots exhausted",
                listenerAccepting,
                clientLoad
            );
        }

        if (entry.ReportedState == "healthy")
        {
            if (
                entry.LastHeartbeatUtc is null
                || now - entry.LastHeartbeatUtc.Value > _stalenessThreshold
            )
            {
                return ToInfo(
                    entry,
                    "unhealthy",
                    $"heartbeat stale (>{_stalenessThreshold.TotalSeconds:0}s)",
                    listenerAccepting,
                    clientLoad
                );
            }
        }
        else if (
            entry.ReportedState == "starting"
            && now - entry.RegisteredAtUtc > _stalenessThreshold
        )
        {
            return ToInfo(
                entry,
                "unhealthy",
                "timed out while starting",
                listenerAccepting,
                clientLoad
            );
        }

        return ToInfo(entry, entry.ReportedState, entry.LastError, listenerAccepting, clientLoad);
    }

    private ProcessHealthInfo EvaluateProcess(
        IReadOnlyDictionary<string, ServerHealthInfo> servers,
        DateTime now
    )
    {
        DateTime lastSchedulerTickUtc;
        TimeSpan lastSchedulerLag;
        double processCpuPercent;

        lock (_processLock)
        {
            lastSchedulerTickUtc = _lastSchedulerTickUtc;
            lastSchedulerLag = _lastSchedulerLag;
            processCpuPercent = _processCpuPercent;
        }

        var totalActiveHandlers = servers.Values.Sum(s => s.ActiveHandlers ?? 0);

        if (lastSchedulerTickUtc == default)
        {
            return new ProcessHealthInfo(
                "unhealthy",
                "scheduler has not ticked yet",
                null,
                null,
                Math.Round(processCpuPercent, 1),
                totalActiveHandlers
            );
        }

        if (now - lastSchedulerTickUtc > _schedulerMaxStale)
        {
            return new ProcessHealthInfo(
                "unhealthy",
                $"scheduler stale (>{_schedulerMaxStale.TotalSeconds:0}s)",
                lastSchedulerTickUtc,
                Math.Round(lastSchedulerLag.TotalSeconds, 2),
                Math.Round(processCpuPercent, 1),
                totalActiveHandlers
            );
        }

        if (lastSchedulerLag > _schedulerMaxLag)
        {
            return new ProcessHealthInfo(
                "unhealthy",
                $"scheduler lag ({lastSchedulerLag.TotalSeconds:0.0}s > {_schedulerMaxLag.TotalSeconds:0}s)",
                lastSchedulerTickUtc,
                Math.Round(lastSchedulerLag.TotalSeconds, 2),
                Math.Round(processCpuPercent, 1),
                totalActiveHandlers
            );
        }

        if (
            _idleMaxProcessCpuPercent > 0
            && totalActiveHandlers <= _idleMaxActiveHandlers
            && processCpuPercent > _idleMaxProcessCpuPercent
        )
        {
            return new ProcessHealthInfo(
                "unhealthy",
                $"high CPU while idle ({processCpuPercent:0}% > {_idleMaxProcessCpuPercent}%)",
                lastSchedulerTickUtc,
                Math.Round(lastSchedulerLag.TotalSeconds, 2),
                Math.Round(processCpuPercent, 1),
                totalActiveHandlers
            );
        }

        return new ProcessHealthInfo(
            "healthy",
            null,
            lastSchedulerTickUtc,
            Math.Round(lastSchedulerLag.TotalSeconds, 2),
            Math.Round(processCpuPercent, 1),
            totalActiveHandlers
        );
    }

    private static ServerHealthInfo ToInfo(
        ServerHealthEntry entry,
        string state,
        string? lastError,
        bool? listenerAccepting,
        VceClientLoad? clientLoad
    ) =>
        new(
            entry.Name,
            entry.Port,
            state,
            lastError,
            entry.LastHeartbeatUtc,
            listenerAccepting,
            clientLoad?.ActiveHandlers,
            clientLoad?.AvailableSlots,
            clientLoad?.MaxHandlers
        );

    private static string ToDisplayName(ServerType serverType) => $"{serverType}Server";

    private static string ToJsonKey(ServerType serverType) =>
        char.ToLowerInvariant(serverType.ToString()[0]) + serverType.ToString()[1..] + "Server";

    private sealed record ServerHealthEntry(
        string Name,
        int Port,
        string ReportedState,
        string? LastError,
        DateTime RegisteredAtUtc,
        DateTime? LastHeartbeatUtc,
        Func<bool>? AcceptCheck = null,
        Func<VceClientLoad>? ClientLoadCheck = null
    );
}

public sealed record ServerHealthInfo(
    string Name,
    int Port,
    string State,
    string? LastError,
    DateTime? LastHeartbeatUtc,
    bool? ListenerAccepting,
    int? ActiveHandlers = null,
    int? AvailableSlots = null,
    int? MaxHandlers = null
);

public sealed record ProcessHealthInfo(
    string State,
    string? LastError,
    DateTime? LastSchedulerTickUtc,
    double? SchedulerLagSeconds,
    double? ProcessCpuPercent,
    int TotalActiveHandlers
);

public sealed record HealthReport(
    IReadOnlyDictionary<string, ServerHealthInfo> Servers,
    ProcessHealthInfo Process
);
