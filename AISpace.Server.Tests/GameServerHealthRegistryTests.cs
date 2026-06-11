using AISpace.Common;
using AISpace.Common.Config;
using AISpace.Server;
using Microsoft.Extensions.Options;

namespace AISpace.Server.Tests;

public class GameServerHealthRegistryTests
{
    private static GameServerHealthRegistry CreateRegistry(int heartbeatSeconds = 5, int stalenessSeconds = 30) =>
        new(Options.Create(new ServerOptions { HealthCheck = new HealthCheckOptions { HeartbeatIntervalSeconds = heartbeatSeconds, StalenessSeconds = stalenessSeconds } }));

    [Fact]
    public void MarkListening_WithFreshHeartbeat_IsHealthy()
    {
        var registry = CreateRegistry();
        registry.AddServer(ServerType.Auth, 50050);
        registry.MarkListening(ServerType.Auth, 50050);

        var snapshot = registry.GetSnapshot();

        Assert.Equal("healthy", snapshot["authServer"].State);
        Assert.NotNull(snapshot["authServer"].LastHeartbeatUtc);
        Assert.Null(snapshot["authServer"].LastError);
    }

    [Fact]
    public void GetSnapshot_WhenHeartbeatStale_ReturnsUnhealthy()
    {
        var registry = CreateRegistry(heartbeatSeconds: 1, stalenessSeconds: 1);
        registry.AddServer(ServerType.Msg, 50052);
        registry.MarkListening(ServerType.Msg, 50052);
        registry.RecordHeartbeat(ServerType.Msg);

        Thread.Sleep(1200);

        var snapshot = registry.GetSnapshot();

        Assert.Equal("unhealthy", snapshot["msgServer"].State);
        Assert.Contains("heartbeat stale", snapshot["msgServer"].LastError, StringComparison.Ordinal);
    }

    [Fact]
    public void GetSnapshot_WhenListenerNotAccepting_ReturnsUnhealthy()
    {
        var registry = CreateRegistry();
        registry.AddServer(ServerType.Area, 50054);
        registry.MarkListening(ServerType.Area, 50054);
        registry.SetAcceptCheck(ServerType.Area, () => false);

        var snapshot = registry.GetSnapshot();

        Assert.Equal("unhealthy", snapshot["areaServer"].State);
        Assert.Equal("listener not accepting", snapshot["areaServer"].LastError);
        Assert.False(snapshot["areaServer"].ListenerAccepting);
    }

    [Fact]
    public void GetSnapshot_WhenListenerAccepting_ReportsListenerState()
    {
        var registry = CreateRegistry();
        registry.AddServer(ServerType.Auth, 50050);
        registry.MarkListening(ServerType.Auth, 50050);
        registry.SetAcceptCheck(ServerType.Auth, () => true);

        var snapshot = registry.GetSnapshot();

        Assert.Equal("healthy", snapshot["authServer"].State);
        Assert.True(snapshot["authServer"].ListenerAccepting);
    }

    [Fact]
    public void GetSnapshot_WhenStartingTooLong_ReturnsUnhealthy()
    {
        var registry = CreateRegistry(heartbeatSeconds: 1, stalenessSeconds: 1);
        Assert.Equal(TimeSpan.FromSeconds(1), registry.StalenessThreshold);

        registry.AddServer(ServerType.Auth, 50050);
        Thread.Sleep(1500);

        var snapshot = registry.GetSnapshot();

        Assert.Equal("unhealthy", snapshot["authServer"].State);
        Assert.Equal("timed out while starting", snapshot["authServer"].LastError);
    }

    [Fact]
    public void MarkUnhealthy_PreservesExplicitReason()
    {
        var registry = CreateRegistry();
        registry.AddServer(ServerType.Auth, 50050);
        registry.MarkUnhealthy(ServerType.Auth, "tcp listener stopped");

        var snapshot = registry.GetSnapshot();

        Assert.Equal("unhealthy", snapshot["authServer"].State);
        Assert.Equal("tcp listener stopped", snapshot["authServer"].LastError);
    }
}
