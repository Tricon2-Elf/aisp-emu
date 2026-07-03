using AISpace.Common;
using AISpace.Common.Config;
using AISpace.Network;
using AISpace.Server;
using Microsoft.Extensions.Options;

namespace AISpace.Server.Tests;

public class GameServerHealthRegistryTests
{
    private static GameServerHealthRegistry CreateRegistry(int heartbeatSeconds = 5, int stalenessSeconds = 30, int idleMaxCpu = 85) =>
        new(
            Options.Create(
                new ServerOptions
                {
                    HealthCheck = new HealthCheckOptions
                    {
                        HeartbeatIntervalSeconds = heartbeatSeconds,
                        StalenessSeconds = stalenessSeconds,
                        IdleMaxProcessCpuPercent = idleMaxCpu,
                    },
                }
            )
        );

    [Fact]
    public void RecordHeartbeat_WhenServerNotRegistered_DoesNotCreateEntry()
    {
        var registry = CreateRegistry();

        registry.RecordHeartbeat(ServerType.Auth);

        Assert.Empty(registry.GetSnapshot());
    }

    [Fact]
    public void RecordHeartbeat_OnlyUpdatesRegisteredServer()
    {
        var registry = CreateRegistry();
        registry.AddServer(ServerType.Msg, 50052);
        registry.MarkListening(ServerType.Msg, 50052);

        registry.RecordHeartbeat(ServerType.Msg);

        var snapshot = registry.GetSnapshot();
        Assert.Single(snapshot);
        Assert.Equal("healthy", snapshot["msgServer"].State);
        Assert.NotNull(snapshot["msgServer"].LastHeartbeatUtc);
    }

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

        var snapshot = WaitForServerState(registry, "msgServer", "unhealthy");

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
        var snapshot = WaitForServerState(registry, "authServer", "unhealthy");

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

    [Fact]
    public void GetHealthReport_WhenSchedulerNotTicked_ProcessIsUnhealthy()
    {
        var registry = CreateRegistry();
        registry.AddServer(ServerType.Auth, 50050);
        registry.MarkListening(ServerType.Auth, 50050);
        registry.RecordHeartbeat(ServerType.Auth);

        var report = registry.GetHealthReport();

        Assert.Equal("unhealthy", report.Process.State);
        Assert.Equal("scheduler has not ticked yet", report.Process.LastError);
        Assert.Equal("healthy", report.Servers["authServer"].State);
    }

    [Fact]
    public void GetHealthReport_AfterInitialSchedulerTick_ProcessCanBeHealthy()
    {
        var registry = CreateRegistry();
        registry.AddServer(ServerType.Auth, 50050);
        registry.MarkListening(ServerType.Auth, 50050);
        registry.RecordHeartbeat(ServerType.Auth);
        registry.RecordSchedulerTick(TimeSpan.Zero);
        registry.SampleProcessCpu();

        var report = registry.GetHealthReport();

        Assert.Equal("healthy", report.Process.State);
    }

    [Fact]
    public void GetHealthReport_WhenSchedulerLagTooHigh_ProcessIsUnhealthy()
    {
        var registry = CreateRegistry();
        registry.AddServer(ServerType.Auth, 50050);
        registry.MarkListening(ServerType.Auth, 50050);
        registry.RecordHeartbeat(ServerType.Auth);
        registry.RecordSchedulerTick(TimeSpan.FromSeconds(12));

        var report = registry.GetHealthReport();

        Assert.Equal("unhealthy", report.Process.State);
        Assert.Contains("scheduler lag", report.Process.LastError, StringComparison.Ordinal);
    }

    [Fact]
    public void GetHealthReport_WhenHighCpuWhileIdle_ProcessIsUnhealthy()
    {
        var registry = new GameServerHealthRegistry(
            Options.Create(
                new ServerOptions
                {
                    HealthCheck = new HealthCheckOptions
                    {
                        HeartbeatIntervalSeconds = 5,
                        StalenessSeconds = 30,
                        IdleMaxProcessCpuPercent = Math.Max(5, (int)(60.0 / Environment.ProcessorCount)),
                        IdleMaxActiveHandlers = 2,
                    },
                }
            )
        );
        registry.AddServer(ServerType.Auth, 50050);
        registry.MarkListening(ServerType.Auth, 50050);
        registry.RecordHeartbeat(ServerType.Auth);
        registry.RecordSchedulerTick(TimeSpan.FromSeconds(1));
        registry.SetClientLoadCheck(ServerType.Auth, () => new VceClientLoad(ActiveHandlers: 1, AvailableSlots: 31, MaxHandlers: 32));

        registry.SampleProcessCpu();
        // Burn CPU on all logical processors so normalized process CPU exceeds the idle threshold
        // even on high-core CI runners.
        var workerCount = Math.Max(1, Environment.ProcessorCount);
        var deadline = Environment.TickCount64 + 700;
        Parallel.For(
            0,
            workerCount,
            _ =>
            {
                while (Environment.TickCount64 < deadline)
                { /* busy spin */
                }
            }
        );

        registry.SampleProcessCpu();

        var report = registry.GetHealthReport();

        Assert.Equal("unhealthy", report.Process.State);
        Assert.Contains("high CPU while idle", report.Process.LastError, StringComparison.Ordinal);
    }

    [Fact]
    public void GetHealthReport_WhenProcessAndServersHealthy_ReturnsHealthy()
    {
        var registry = CreateRegistry();
        registry.AddServer(ServerType.Auth, 50050);
        registry.MarkListening(ServerType.Auth, 50050);
        registry.RecordHeartbeat(ServerType.Auth);
        registry.RecordSchedulerTick(TimeSpan.FromSeconds(1));
        registry.SampleProcessCpu();

        var report = registry.GetHealthReport();

        Assert.Equal("healthy", report.Process.State);
        Assert.Equal("healthy", report.Servers["authServer"].State);
    }

    private static IReadOnlyDictionary<string, ServerHealthInfo> WaitForServerState(GameServerHealthRegistry registry, string serverKey, string expectedState, int timeoutMs = 5000, int pollMs = 25)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        IReadOnlyDictionary<string, ServerHealthInfo>? snapshot = null;
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            snapshot = registry.GetSnapshot();
            if (snapshot.TryGetValue(serverKey, out var info) && info.State == expectedState)
                return snapshot;

            Thread.Sleep(pollMs);
        }

        snapshot ??= registry.GetSnapshot();
        var actual = snapshot.TryGetValue(serverKey, out var finalInfo) ? finalInfo.State : "<missing>";
        throw new Xunit.Sdk.XunitException($"Timed out waiting for {serverKey} to become '{expectedState}'. Last state was '{actual}'.");
    }
}
