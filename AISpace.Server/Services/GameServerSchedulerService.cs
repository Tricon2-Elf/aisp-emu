using AISpace.Common;
using AISpace.Common.Config;
using AISpace.Common.Game;
using AISpace.Common.Services;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Options;

namespace AISpace.Server.Services;

/// <summary>Single process-wide timer for health heartbeats and area timezone broadcasts.</summary>
public sealed class GameServerSchedulerService(SharedState state, GameServerHealthRegistry healthRegistry, IOptions<ServerOptions> options, ILogger<GameServerSchedulerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var heartbeatIntervalSeconds = Math.Max(1, options.Value.HealthCheck.HeartbeatIntervalSeconds);
        logger.LogInformation("Game scheduler started (1s tick, heartbeat every {HeartbeatSeconds}s)", heartbeatIntervalSeconds);

        var tick = 0;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(ct))
        {
            tick++;
            if (tick % heartbeatIntervalSeconds == 0)
                healthRegistry.RecordHeartbeatsForRegisteredServers();

            BroadcastAreaTimeIfNeeded();
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
