using AISpace.Common;
using AISpace.Common.Config;
using AISpace.Network;
using AISpace.Server;
using Microsoft.Extensions.Options;

namespace AISpace.Server.Tests;

public class GameServerHealthRegistryTests
{
    private static GameServerHealthRegistry CreateRegistry(int heartbeatSeconds = 5, int stalenessSeconds = 30) =>
        new(
            Options.Create(
                new ServerOptions
                {
                    HealthCheck = new HealthCheckOptions { HeartbeatIntervalSeconds = heartbeatSeconds, StalenessSeconds = stalenessSeconds },
                }
            )
        );

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

    [Fact]
    public void GetSnapshot_IncludesClientLoadMetrics()
    {
        var registry = CreateRegistry();
        registry.AddServer(ServerType.Auth, 50050);
        registry.MarkListening(ServerType.Auth, 50050);
        registry.SetClientLoadCheck(ServerType.Auth, () => new VceClientLoad(ActiveHandlers: 7, AvailableSlots: 25, MaxHandlers: 32));

        var snapshot = registry.GetSnapshot();

        Assert.Equal(7, snapshot["authServer"].ActiveHandlers);
        Assert.Equal(25, snapshot["authServer"].AvailableSlots);
        Assert.Equal(32, snapshot["authServer"].MaxHandlers);
    }

    [Fact]
    public void GetSnapshot_WhenClientHandlerSlotsExhausted_ReturnsUnhealthy()
    {
        var registry = CreateRegistry();
        registry.AddServer(ServerType.Auth, 50050);
        registry.MarkListening(ServerType.Auth, 50050);
        registry.SetClientLoadCheck(ServerType.Auth, () => new VceClientLoad(ActiveHandlers: 32, AvailableSlots: 0, MaxHandlers: 32));

        var snapshot = registry.GetSnapshot();

        Assert.Equal("unhealthy", snapshot["authServer"].State);
        Assert.Equal("client handler slots exhausted", snapshot["authServer"].LastError);
        Assert.Equal(0, snapshot["authServer"].AvailableSlots);
    }

    [Fact]
    public void GetSnapshot_WhenOneSlotBelowCapacity_RemainsHealthy()
    {
        var registry = CreateRegistry();
        registry.AddServer(ServerType.Auth, 50050);
        registry.MarkListening(ServerType.Auth, 50050);
        registry.SetClientLoadCheck(ServerType.Auth, () => new VceClientLoad(ActiveHandlers: 31, AvailableSlots: 1, MaxHandlers: 32));

        var snapshot = registry.GetSnapshot();

        Assert.Equal("healthy", snapshot["authServer"].State);
        Assert.Null(snapshot["authServer"].LastError);
        Assert.Equal(31, snapshot["authServer"].ActiveHandlers);
        Assert.Equal(1, snapshot["authServer"].AvailableSlots);
    }
}
