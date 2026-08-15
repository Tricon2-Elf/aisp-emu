namespace aisp.Common.Config;

public class MaintenanceOptions
{
    public bool Enabled { get; set; }

    /// <summary>Scheduled restart time in "HH:mm" format (UTC).</summary>
    public string ScheduledTime { get; set; } = "04:00";

    /// <summary>Minutes before shutdown at which warning broadcasts are sent (e.g. [30, 15, 10, 5, 1]).</summary>
    public int[] WarningMinutes { get; set; } = [30, 15, 10, 5, 1];

    /// <summary>Warning message template. {0} is replaced with the minutes remaining.</summary>
    public string Message { get; set; } =
        "Server maintenance in {0} minute(s). You will be disconnected.";
}
