using AISpace.Common;
using AISpace.Common.Game;
using AISpace.Network;

namespace AISpace.Server;

public abstract class GameServerBase<T> : BackgroundService
    where T : GameServerBase<T>
{
    protected readonly ILogger<T> Logger;

    protected readonly SharedState State;
    protected readonly MainContext Db;
    protected readonly PacketDispatcher Dispatcher;
    protected readonly IUserRepository UserRepo;
    protected readonly IWorldRepository WorldRepo;
    protected readonly int _port;
    protected readonly ILoggerFactory _loggerFactory;
    protected readonly TimeSpan TickRate;

    protected abstract ServerType ActiveServerType { get; }

    protected readonly GameServerHealthRegistry HealthRegistry;

    private readonly int _maxConcurrentClients;
    private readonly int _maxReceiveFrameSize;
    private readonly int _packetChannelCapacity;
    private bool _initialized;
    private DateTime _lastHeartbeatUtc = DateTime.MinValue;

    protected GameServerBase(ILogger<T> logger, GameServerContext ctx, int port)
    {
        Logger = logger;
        Db = ctx.Db;
        UserRepo = ctx.UserRepo;
        _port = port;
        _loggerFactory = ctx.LoggerFactory;
        WorldRepo = ctx.WorldRepo;
        Dispatcher = ctx.Dispatcher;
        State = ctx.State;
        HealthRegistry = ctx.HealthRegistry;
        _maxConcurrentClients = ctx.MaxConcurrentClients;
        _maxReceiveFrameSize = ctx.MaxReceiveFrameSize;
        _packetChannelCapacity = ctx.PacketChannelCapacity;
        TickRate = TimeSpan.FromMilliseconds(1000.0 / Math.Max(1, ctx.TickRateHz));
        HealthRegistry.AddServer(ActiveServerType, _port);
    }

    protected virtual Task InitializeAsync(CancellationToken ct) => Task.CompletedTask;

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

            var listener = new VceListener(_loggerFactory.CreateLogger<VceListener>(), channel, ActiveServerType.ToString(), _port, _loggerFactory, id => State.UnregisterClient(ActiveServerType, id), (_, p) => HealthRegistry.MarkListening(ActiveServerType, p), _maxConcurrentClients, _maxReceiveFrameSize);
            HealthRegistry.SetAcceptCheck(ActiveServerType, () => listener.IsListening);

            var acceptLoop = listener.RunAsync(runToken);
            var packetLoop = RunPacketLoop(channel.Reader, runToken);
            var gameLoop = RunGameLoop(runToken);

            var completed = await Task.WhenAny(acceptLoop, packetLoop);
            if (completed == acceptLoop)
            {
                Logger.LogWarning("{ServerType} TCP listener stopped unexpectedly; restarting", ActiveServerType);
                HealthRegistry.MarkUnhealthy(ActiveServerType, "tcp listener stopped");
            }
            else
            {
                Logger.LogWarning("{ServerType} packet loop stopped unexpectedly; restarting listener", ActiveServerType);
                HealthRegistry.MarkUnhealthy(ActiveServerType, "packet loop stopped");
            }

            runCts.Cancel();
            HealthRegistry.ClearAcceptCheck(ActiveServerType);
            await Task.WhenAll(acceptLoop.ContinueWith(_ => Task.CompletedTask, TaskScheduler.Default), packetLoop.ContinueWith(_ => Task.CompletedTask, TaskScheduler.Default), gameLoop.ContinueWith(_ => Task.CompletedTask, TaskScheduler.Default));

            if (ct.IsCancellationRequested)
                break;

            await Task.Delay(TimeSpan.FromSeconds(1), ct);
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
                    var session = State.GetOrAddSession(packet.Client.Id, () => new PlayerSession(packet.Client.Id, packet.Client));
                    await Dispatcher.DispatchAsync(ActiveServerType, packet.Type, packet.Data, session, ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Packet dispatch failed (ServerType={ServerType}, type={Type}): {Message}", ActiveServerType, packet.Type, ex.Message);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // expected during listener restart
        }
    }

    private async Task RunGameLoop(CancellationToken ct)
    {
        try
        {
            using var timer = new PeriodicTimer(TickRate);
            while (await timer.WaitForNextTickAsync(ct))
            {
                try
                {
                    var now = DateTime.UtcNow;
                    if (now - _lastHeartbeatUtc >= HealthRegistry.HeartbeatInterval)
                    {
                        HealthRegistry.RecordHeartbeat(ActiveServerType);
                        _lastHeartbeatUtc = now;
                    }

                    OnTick(ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Game tick failed (ServerType={ServerType}): {Message}", ActiveServerType, ex.Message);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // expected during listener restart
        }
    }

    protected virtual void OnTick(CancellationToken ct) { }
}
