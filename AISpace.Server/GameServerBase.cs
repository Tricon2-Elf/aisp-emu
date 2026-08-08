using AISpace.Common;
using AISpace.Common.Game;
using AISpace.Network;
using Microsoft.Extensions.DependencyInjection;

namespace AISpace.Server;

public abstract class GameServerBase<T> : BackgroundService
    where T : GameServerBase<T>
{
    protected readonly ILogger<T> Logger;

    protected readonly SharedState State;
    protected readonly PacketDispatcher Dispatcher;
    protected readonly IServiceScopeFactory ScopeFactory;
    protected readonly int _port;
    protected readonly ILoggerFactory _loggerFactory;

    protected abstract ServerType ActiveServerType { get; }

    protected readonly GameServerHealthRegistry HealthRegistry;

    private readonly int _maxConcurrentClients;
    private readonly int _maxReceiveFrameSize;
    private readonly int _clientReadTimeoutSeconds;
    private readonly int _packetChannelCapacity;
    private bool _initialized;

    protected GameServerBase(ILogger<T> logger, GameServerContext ctx, int port)
    {
        Logger = logger;
        ScopeFactory = ctx.ScopeFactory;
        _port = port;
        _loggerFactory = ctx.LoggerFactory;
        Dispatcher = ctx.Dispatcher;
        State = ctx.State;
        HealthRegistry = ctx.HealthRegistry;
        _maxConcurrentClients = ctx.MaxConcurrentClients;
        _maxReceiveFrameSize = ctx.MaxReceiveFrameSize;
        _clientReadTimeoutSeconds = ctx.ClientReadTimeoutSeconds;
        _packetChannelCapacity = ctx.PacketChannelCapacity;
        HealthRegistry.AddServer(ActiveServerType, _port);
    }

    protected virtual Task InitializeAsync(CancellationToken ct) => Task.CompletedTask;

    protected virtual IEnumerable<Task> GetAdditionalLoops(CancellationToken ct) => [];

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        Logger.LogInformation("Starting {ServerType} server", ActiveServerType);

        while (!ct.IsCancellationRequested)
        {
            if (!_initialized)
            {
                await InitializeAsync(ct);
                _initialized = true;
            }

            using var runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var runToken = runCts.Token;

            var channelOpts = new BoundedChannelOptions(_packetChannelCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
            };
            var channel = System.Threading.Channels.Channel.CreateBounded<Packet>(channelOpts);

            var listener = new VceListener(
                _loggerFactory.CreateLogger<VceListener>(),
                channel,
                ActiveServerType.ToString(),
                _port,
                _loggerFactory,
                id =>
                {
                    if (
                        State.TryGetSession(id, out var disconnectedSession)
                        && disconnectedSession is not null
                    )
                    {
                        if (ActiveServerType == ServerType.Area)
                        {
                            try
                            {
                                State
                                    .BroadcastAreaDisappearAsync(disconnectedSession)
                                    .GetAwaiter()
                                    .GetResult();
                            }
                            catch (Exception ex)
                            {
                                Logger.LogWarning(
                                    ex,
                                    "Failed broadcasting area disconnect disappear for client {ClientId}",
                                    id
                                );
                            }
                        }

                        if (
                            ActiveServerType == ServerType.Msg
                            && disconnectedSession.CharacterId != 0
                        )
                        {
                            try
                            {
                                using var scope = ScopeFactory.CreateScope();
                                var circles =
                                    scope.ServiceProvider.GetRequiredService<AISpace.Common.DAL.Repositories.ICircleRepository>();
                                AISpace
                                    .Common.Handlers.Msg.CircleNotifyHelper.NotifyMemberLogoutAsync(
                                        circles,
                                        State,
                                        (int)disconnectedSession.CharacterId,
                                        CancellationToken.None
                                    )
                                    .GetAwaiter()
                                    .GetResult();
                            }
                            catch (Exception ex)
                            {
                                Logger.LogWarning(
                                    ex,
                                    "Failed broadcasting circle logout for client {ClientId}",
                                    id
                                );
                            }
                        }
                    }

                    State.UnregisterClient(ActiveServerType, id);
                },
                (_, p) => HealthRegistry.MarkListening(ActiveServerType, p),
                _maxConcurrentClients,
                _maxReceiveFrameSize,
                _clientReadTimeoutSeconds,
                id =>
                {
                    if (!State.TryGetSession(id, out var session) || session is null)
                        return null;

                    var userId = session.User?.Id ?? session.UserId;
                    return userId > 0 ? userId : null;
                }
            );
            HealthRegistry.SetAcceptCheck(ActiveServerType, () => listener.IsListening);
            HealthRegistry.SetClientLoadCheck(ActiveServerType, () => listener.GetClientLoad());

            var acceptLoop = listener.RunAsync(runToken);
            var packetLoop = RunPacketLoop(channel.Reader, runToken);
            var heartbeatLoop = RunHeartbeatLoop(runToken);
            var additionalLoops = GetAdditionalLoops(runToken).ToArray();

            var allLoops = new List<Task>(3 + additionalLoops.Length)
            {
                acceptLoop,
                packetLoop,
                heartbeatLoop,
            };
            allLoops.AddRange(additionalLoops);

            var completed = await Task.WhenAny(allLoops);
            if (completed == acceptLoop)
            {
                Logger.LogWarning(
                    "{ServerType} TCP listener stopped unexpectedly; restarting",
                    ActiveServerType
                );
                HealthRegistry.MarkUnhealthy(ActiveServerType, "tcp listener stopped");
            }
            else if (completed == packetLoop)
            {
                Logger.LogWarning(
                    "{ServerType} packet loop stopped unexpectedly; restarting listener",
                    ActiveServerType
                );
                HealthRegistry.MarkUnhealthy(ActiveServerType, "packet loop stopped");
            }
            else if (completed == heartbeatLoop)
            {
                Logger.LogWarning(
                    "{ServerType} heartbeat loop stopped unexpectedly; restarting listener",
                    ActiveServerType
                );
                HealthRegistry.MarkUnhealthy(ActiveServerType, "heartbeat loop stopped");
            }
            else
            {
                Logger.LogWarning(
                    "{ServerType} auxiliary loop stopped unexpectedly; restarting listener",
                    ActiveServerType
                );
                HealthRegistry.MarkUnhealthy(ActiveServerType, "auxiliary loop stopped");
            }

            runCts.Cancel();
            HealthRegistry.ClearAcceptCheck(ActiveServerType);
            HealthRegistry.ClearClientLoadCheck(ActiveServerType);
            await Task.WhenAll(
                allLoops.Select(task =>
                    task.ContinueWith(_ => Task.CompletedTask, TaskScheduler.Default)
                )
            );

            if (ct.IsCancellationRequested)
                break;

            await Task.Delay(TimeSpan.FromSeconds(1), ct);
        }
    }

    private async Task RunHeartbeatLoop(CancellationToken ct)
    {
        try
        {
            using var timer = new PeriodicTimer(HealthRegistry.HeartbeatInterval);
            while (await timer.WaitForNextTickAsync(ct))
                HealthRegistry.RecordHeartbeat(ActiveServerType);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // expected during listener restart
        }
    }

    private async Task RunPacketLoop(ChannelReader<Packet> channel, CancellationToken ct)
    {
        try
        {
            await foreach (var packet in channel.ReadAllAsync(ct))
            {
                try
                {
                    if (packet.Client.IsClosed)
                    {
                        Logger.LogDebug(
                            "Dropping packet {Type} for closed client {ClientId} on {ServerType}",
                            packet.Type,
                            packet.Client.Id,
                            ActiveServerType
                        );
                        continue;
                    }

                    IPlayerSession? session;
                    if (!State.TryGetSession(packet.Client.Id, out session) || session is null)
                    {
                        // Do not resurrect sessions for clients that disconnected while packets were still queued.
                        if (packet.Client.IsClosed)
                        {
                            Logger.LogDebug(
                                "Skipping stale queued packet {Type} after disconnect for client {ClientId} on {ServerType}",
                                packet.Type,
                                packet.Client.Id,
                                ActiveServerType
                            );
                            continue;
                        }

                        session = State.GetOrAddSession(
                            packet.Client.Id,
                            () => new PlayerSession(packet.Client.Id, packet.Client)
                        );
                    }

                    await Dispatcher.DispatchAsync(
                        ActiveServerType,
                        packet.Type,
                        packet.Data,
                        session,
                        ct
                    );
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.LogError(
                        ex,
                        "Packet dispatch failed (ServerType={ServerType}, type={Type}): {Message}",
                        ActiveServerType,
                        packet.Type,
                        ex.Message
                    );
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // expected during listener restart
        }
    }
}
