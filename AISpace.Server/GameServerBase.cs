using AISpace.Common;
using AISpace.Common.Game;

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
    protected readonly ChannelReader<Packet> Channel;
    protected readonly Channel<Packet> _channel;
    protected readonly int _port;
    protected readonly ILoggerFactory _loggerFactory;
    protected readonly TimeSpan TickRate = TimeSpan.FromMilliseconds(1000.0 / 60.0);

    protected abstract ServerType ActiveServerType { get; }

    protected readonly GameServerHealthRegistry HealthRegistry;

    private readonly int _maxConcurrentClients;

    protected GameServerBase(ILogger<T> logger, GameServerContext ctx, int port)
    {
        Logger = logger;
        Db = ctx.Db;
        UserRepo = ctx.UserRepo;
        var channelOpts = new BoundedChannelOptions(ctx.PacketChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        };
        _channel = System.Threading.Channels.Channel.CreateBounded<Packet>(channelOpts);
        Channel = _channel.Reader;
        _port = port;
        _loggerFactory = ctx.LoggerFactory;
        WorldRepo = ctx.WorldRepo;
        Dispatcher = ctx.Dispatcher;
        State = ctx.State;
        HealthRegistry = ctx.HealthRegistry;
        _maxConcurrentClients = ctx.MaxConcurrentClients;
        HealthRegistry.AddServer(ActiveServerType, _port);
    }

    protected virtual Task InitializeAsync(CancellationToken ct) => Task.CompletedTask;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        Logger.LogInformation("Starting {ServerType} server", ActiveServerType);
        await InitializeAsync(ct);
        var listener = new VceListener(_loggerFactory.CreateLogger<VceListener>(), _channel, ActiveServerType.ToString(), _port, _loggerFactory, id => State.UnregisterClient(ActiveServerType, id), (_, p) => HealthRegistry.MarkListening(ActiveServerType, p), _maxConcurrentClients);
        var packetLoop = RunPacketLoop(ct);
        var acceptLoop = listener.RunAsync(ct);
        var gameLoop = RunGameLoop(ct);
        await Task.WhenAll(packetLoop, acceptLoop, gameLoop);
    }

    private async Task RunPacketLoop(CancellationToken ct)
    {
        await foreach (var packet in Channel.ReadAllAsync(ct))
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

    private async Task RunGameLoop(CancellationToken ct)
    {
        var timer = new PeriodicTimer(TickRate);
        while (await timer.WaitForNextTickAsync(ct))
        {
            try
            {
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

    protected virtual void OnTick(CancellationToken ct) { }
}
