using aisp.Common;
using aisp.Common.Config;
using aisp.Common.Game;
using aisp.Common.Services;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Options;

namespace aisp.Server.Services;

/// <summary>Single process-wide timer for scheduler health sampling and area timezone broadcasts.</summary>
public sealed class GameServerSchedulerService(
    SharedState state,
    GameServerHealthRegistry healthRegistry,
    IOptions<ServerOptions> options,
    ILogger<GameServerSchedulerService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var heartbeatIntervalSeconds = Math.Max(
            1,
            options.Value.HealthCheck.HeartbeatIntervalSeconds
        );
        var schedulerTickSeconds = Math.Max(1, options.Value.HealthCheck.SchedulerTickSeconds);
        logger.LogInformation("Game scheduler started ({TickSeconds}s tick)", schedulerTickSeconds);

        var tick = 0;
        var lastTickUtc = DateTime.UtcNow;
        healthRegistry.RecordSchedulerTick(TimeSpan.Zero);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(schedulerTickSeconds));
        while (await timer.WaitForNextTickAsync(ct))
        {
            var now = DateTime.UtcNow;
            var lag = now - lastTickUtc;
            lastTickUtc = now;

            RunTickStep(
                "scheduler tick",
                () =>
                {
                    healthRegistry.RecordSchedulerTick(lag);
                    healthRegistry.SampleProcessCpu();
                }
            );

            tick++;
            if (tick % heartbeatIntervalSeconds == 0)
                RunTickStep("area time broadcast", BroadcastAreaTimeIfNeeded);
        }
    }

    private void RunTickStep(string stepName, Action action)
    {
        try
        {
            action();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Game scheduler {StepName} failed: {Message}",
                stepName,
                ex.Message
            );
        }
    }

    private void BroadcastAreaTimeIfNeeded()
    {
        if (!healthRegistry.IsRegistered(ServerType.Area))
            return;

        var clients = state.GetServerClients(ServerType.Area);
        if (!clients.Any(client => client.IsAuthenticated))
            return;

        var t = TimeZoneService.GetServerTime();
        var data = new TimeZoneGetResponse(0, (uint)t.Phase, t.Current, t.Max, 0).ToBytes();

        foreach (var client in clients)
        {
            if (client.IsAuthenticated)
                _ = client.SendAsync(PacketType.TimeZoneGetResponse, data);
        }
    }
}
