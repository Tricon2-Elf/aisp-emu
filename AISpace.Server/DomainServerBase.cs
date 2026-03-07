using AISpace.Common;
using AISpace.Common.Game;

namespace AISpace.Server;

public abstract class DomainServerBase<T> : BackgroundService
    where T : DomainServerBase<T>
{
    protected readonly ILogger<T> Logger;

    protected readonly SharedState State;
    protected readonly MainContext Db;
    protected readonly PacketDispatcher Dispatcher;
    protected readonly IUserRepository UserRepo;
    protected readonly IWorldRepository WorldRepo;
    protected readonly ChannelReader<Packet> Channel;
    protected readonly TimeSpan TickRate = TimeSpan.FromMilliseconds(1000.0 / 60.0);

    protected abstract MessageDomain ActiveDomain { get; }

    protected DomainServerBase(ILogger<T> logger, MainContext db, IUserRepository userRepo, ChannelReader<Packet> channel, IWorldRepository worldRepo, PacketDispatcher dispatcher, SharedState state)
    {
        Logger = logger;
        Db = db;
        UserRepo = userRepo;
        Channel = channel;
        WorldRepo = worldRepo;
        Dispatcher = dispatcher;
        State = state;
        Db.Database.EnsureCreated();
        Initialize();
    }

    protected virtual void Initialize() { }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        Logger.LogInformation("Starting {domain} server", ActiveDomain);
        var packetLoop = RunPacketLoop(ct);
        var gameLoop = RunGameLoop(ct);
        await Task.WhenAll(packetLoop, gameLoop);
    }

    private async Task RunPacketLoop(CancellationToken ct)
    {
        await foreach (var packet in Channel.ReadAllAsync(ct))
        {
            try
            {
                await Dispatcher.DispatchAsync(ActiveDomain, packet.Type, packet.Data, packet.Client, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Packet dispatch failed (domain={Domain}, type={Type}): {Message}", ActiveDomain, packet.Type, ex.Message);
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
                Logger.LogError(ex, "Game tick failed (domain={Domain}): {Message}", ActiveDomain, ex.Message);
            }
        }
    }

    protected virtual void OnTick(CancellationToken ct) { }
}
