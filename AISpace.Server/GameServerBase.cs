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
    protected readonly string _serverName;
    protected readonly ILoggerFactory _loggerFactory;
    protected readonly TimeSpan TickRate = TimeSpan.FromMilliseconds(1000.0 / 60.0);

    protected abstract ServerType ActiveServerType { get; }

    protected readonly GameServerHealthRegistry HealthRegistry;
    protected readonly string _healthKey;

    private readonly int _maxConcurrentClients;

    protected GameServerBase(ILogger<T> logger, MainContext db, IUserRepository userRepo, int port, string serverName, ILoggerFactory loggerFactory, IWorldRepository worldRepo, PacketDispatcher dispatcher, SharedState state, GameServerHealthRegistry healthRegistry, int maxConcurrentClients, string healthKey)
    {
        Logger = logger;
        Db = db;
        UserRepo = userRepo;
        _channel = System.Threading.Channels.Channel.CreateUnbounded<Packet>();
        Channel = _channel.Reader;
        _port = port;
        _serverName = serverName;
        _loggerFactory = loggerFactory;
        WorldRepo = worldRepo;
        Dispatcher = dispatcher;
        State = state;
        HealthRegistry = healthRegistry;
        _maxConcurrentClients = maxConcurrentClients;
        _healthKey = healthKey;
        HealthRegistry.AddServer(_healthKey, _port);
        Db.Database.EnsureCreated();
        Initialize();
    }

    protected virtual void Initialize() { }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        Logger.LogInformation("Starting {ServerType} server", ActiveServerType);
        var listener = new VceListener(_loggerFactory.CreateLogger<VceListener>(), _channel, _serverName, _port, _loggerFactory, id => State.UnregisterClient(ActiveServerType, id), (_, p) => HealthRegistry.MarkListening(_healthKey, p), _maxConcurrentClients);
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
