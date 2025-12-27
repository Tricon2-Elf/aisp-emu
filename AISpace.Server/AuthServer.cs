using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Server;

public class AuthServer : BackgroundService
{
    private readonly ILogger<AuthServer> _logger;
    private readonly MainContext _db;
    private readonly PacketDispatcher _dispatcher;
    private readonly IUserRepository _userRepo;
    private readonly IWorldRepository _worldRepo;
    private readonly ChannelReader<Packet> _channel;
    public readonly MessageDomain ActiveDomain = MessageDomain.Auth;

    private readonly TimeSpan _tickRate = TimeSpan.FromMilliseconds(1000.0 / 60.0);

    public AuthServer(ILogger<AuthServer> logger,
        MainContext db,
        IUserRepository userRepo,
        AuthChannel channel,
        IWorldRepository worldRepo,
        PacketDispatcher dispatcher)
    {
        _logger = logger;
        _db = db;
        _channel = channel.Channel;
        _dispatcher = dispatcher;
        _userRepo = userRepo;
        _worldRepo = worldRepo;

        //Setup DB. Since dev just nuke and recreate
        //Nuke DB
        //_db.Database.EnsureDeleted();
        //Create DB
        _db.Database.EnsureCreated();

        if(db.Worlds.Any() == false)
            _worldRepo.AddAsync("default", "Localhost World", "127.0.0.1", 50052);
        if(db.Users.Any() == false)
            _userRepo.AddAsync("testuser", "password");
    }

    protected override async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting {domain} server", ActiveDomain);
        var packetLoop = RunPacketLoop(ct);
        var gameLoop = RunGameLoop(ct);

        await Task.WhenAll(packetLoop, gameLoop);
    }

    private async Task RunPacketLoop(CancellationToken ct = default)
    {
        await foreach (var packet in _channel.ReadAllAsync(ct))
        {
            await _dispatcher.DispatchAsync(ActiveDomain, packet.Type, packet.Data, packet.Client, ct);
        }
    }

    private async Task RunGameLoop(CancellationToken ct = default)
    {
        var sw = new PeriodicTimer(_tickRate);
        while (await sw.WaitForNextTickAsync(ct))
        {
            // Advance game simulation
            UpdateWorld();
        }
    }
    private void UpdateWorld()
    {
        // game state update logic goes here
    }
}
