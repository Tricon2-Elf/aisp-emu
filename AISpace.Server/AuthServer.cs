using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;
using AISpace.Common.Network.Handlers;
using AISpace.Common.Network.Packets;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;

namespace AISpace.Server;

public class AuthServer : BackgroundService
{
    private readonly ILogger<AuthServer> _logger;
    private readonly MainContext _db;
    private readonly IUserRepository _userRepo;
    private readonly IWorldRepository _worldRepo;
    private readonly PacketDispatcher _dispatcher;
    private readonly AuthChannel _authChannel;

    public AuthServer(ILogger<AuthServer> logger, MainContext db, IUserRepository userRepo, AuthChannel channel, IWorldRepository worldRepo, PacketDispatcher dispatcher)
    {
        _logger = logger;
        _db = db;
        _userRepo = userRepo;
        _worldRepo = worldRepo;
        _dispatcher = dispatcher;
        _authChannel = channel;
        
        _db.Database.EnsureCreated();
        InitDatabase().Wait();
    }

    private async Task InitDatabase()
    {
        if (!await _db.Worlds.AnyAsync()) 
        {
            await _worldRepo.AddAsync("Local", "AI Sp@ce Server", "127.0.0.1", 50052);
        }

        if (!await _db.Users.AnyAsync())
        {
            _logger.LogInformation("Database empty. Creating default test user/password");
            await _userRepo.AddAsync("testuser", "password");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Auth server loop started.");
        
        await foreach (var packet in _authChannel.Channel.Reader.ReadAllAsync(ct)) 
        {
            await _dispatcher.DispatchAsync(MessageDomain.Auth, packet.Type, packet.Data, packet.Client, ct);
        }
    }
}
