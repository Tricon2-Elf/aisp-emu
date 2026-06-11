namespace AISpace.Common.Config;

public class HealthCheckOptions
{
    /// <summary>How often each game server records a heartbeat while its game loop is running.</summary>
    public int HeartbeatIntervalSeconds { get; set; } = 5;

    /// <summary>Maximum age of the last heartbeat (or startup grace period) before /healthz reports unhealthy.</summary>
    public int StalenessSeconds { get; set; } = 30;
}
