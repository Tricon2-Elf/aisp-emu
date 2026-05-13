using AISpace.Common;
using AISpace.Common.Config;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Options;

namespace AISpace.Server;

public class ScheduledMaintenanceService(SharedState state, IOptions<MaintenanceOptions> options, IHostApplicationLifetime lifetime, ILogger<ScheduledMaintenanceService> logger) : BackgroundService
{
    private readonly MaintenanceOptions _options = options.Value;
    private readonly HashSet<int> _sentWarnings = [];
    private bool _shutdownTriggered;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting ScheduledMaintenanceService");
        if (!_options.Enabled)
        {
            logger.LogInformation("Scheduled maintenance is disabled");
            return;
        }

        var parsed = TimeOnly.TryParseExact(_options.ScheduledTime, "HH:mm", out var scheduledTimeOfDay);
        if (!parsed)
        {
            logger.LogError("Invalid ScheduledTime format '{ScheduledTime}'. Expected 'HH:mm' (UTC). Maintenance disabled.", _options.ScheduledTime);
            return;
        }

        logger.LogInformation("Scheduled maintenance enabled: daily at {ScheduledTime} UTC with warnings at {Minutes} min", _options.ScheduledTime, string.Join(", ", _options.WarningMinutes));

        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var scheduleTime = new DateTimeOffset(now.Year, now.Month, now.Day, scheduledTimeOfDay.Hour, scheduledTimeOfDay.Minute, 0, TimeSpan.Zero);

            while (now > scheduleTime)
                scheduleTime = scheduleTime.AddDays(1);

            var timeUntil = scheduleTime - now;

            foreach (var warningMin in _options.WarningMinutes.OrderByDescending(m => m))
            {
                if (_sentWarnings.Contains(warningMin))
                    continue;

                if (timeUntil.TotalMinutes <= warningMin)
                {
                    _sentWarnings.Add(warningMin);
                    BroadcastWarning(warningMin);
                }
            }

            if (timeUntil.TotalSeconds <= 0 && !_shutdownTriggered)
            {
                _shutdownTriggered = true;
                BroadcastShutdown();
                logger.LogInformation("Scheduled shutdown triggered. Stopping application.");
                lifetime.StopApplication();
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(15), ct);
        }
    }

    private void BroadcastWarning(int minutesRemaining)
    {
        var message = minutesRemaining == 1 ? "Server maintenance in 1 minute. You will be disconnected." : string.Format(_options.Message, minutesRemaining);

        logger.LogInformation("Broadcasting maintenance warning: {Minutes} min remaining", minutesRemaining);
        SendToAllAreaClients(message);
    }

    private void BroadcastShutdown()
    {
        const string message = "The server is shutting down for scheduled maintenance.";
        logger.LogInformation("Broadcasting shutdown notice");
        SendToAllAreaClients(message);
    }

    private void SendToAllAreaClients(string message)
    {
        var notify = new EventMessageNotify(0, "System", message);
        var data = notify.ToBytes();

        foreach (var client in state.GetServerClients(ServerType.Area))
        {
            if (client.IsAuthenticated)
                _ = client.SendAsync(PacketType.EventMessageNotify, data);
        }
    }
}
