namespace aisp.Common.Config;

public class HealthCheckOptions
{
    /// <summary>How often each game server records a heartbeat while its listener loop is running.</summary>
    public int HeartbeatIntervalSeconds { get; set; } = 5;

    /// <summary>Maximum age of the last heartbeat (or startup grace period) before /healthz reports unhealthy.</summary>
    public int StalenessSeconds { get; set; } = 30;

    /// <summary>Expected scheduler tick period in seconds (GameServerSchedulerService). Gaps larger than SchedulerMaxLagSeconds mark the process unhealthy.</summary>
    public int SchedulerTickSeconds { get; set; } = 1;

    /// <summary>Maximum allowed gap between scheduler ticks before /healthz reports process unhealthy.</summary>
    public int SchedulerMaxLagSeconds { get; set; } = 5;

    /// <summary>Maximum age of the last scheduler tick before /healthz reports process unhealthy.</summary>
    public int SchedulerMaxStaleSeconds { get; set; } = 10;

    /// <summary>When total active client handlers is at or below IdleMaxActiveHandlers, sustained CPU above this percent marks the process unhealthy. Set to 0 to disable.</summary>
    public int IdleMaxProcessCpuPercent { get; set; } = 85;

    /// <summary>Total active handlers at or below this value is treated as idle for IdleMaxProcessCpuPercent.</summary>
    public int IdleMaxActiveHandlers { get; set; } = 2;
}
