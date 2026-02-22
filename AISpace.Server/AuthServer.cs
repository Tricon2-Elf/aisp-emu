using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;
using AISpace.Common.Network.Handlers;
using AISpace.Common.Network.Packets;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Server;

public class AuthServer : BackgroundService
{
    private readonly ILogger<AuthServer> _logger;
    private readonly MainContext _db;
    private readonly IUserRepository _userRepo;
    private readonly IWorldRepository _worldRepo;
    private readonly ICharacterRepository _charRepo;
    private readonly ChannelReader<Packet> _channel;
    private readonly PacketDispatcher _dispatcher;

    public AuthServer(ILogger<AuthServer> logger, MainContext db, IUserRepository userRepo, 
                      AuthChannel channel, IWorldRepository worldRepo, 
                      ICharacterRepository charRepo, PacketDispatcher dispatcher)
    {
        _logger = logger;
        _db = db;
        _channel = channel.Channel;
        _worldRepo = worldRepo;
        _userRepo = userRepo;
        _charRepo = charRepo;
        _dispatcher = dispatcher;

        _db.Database.EnsureCreated();
        InitDatabase().Wait();
    }

    private async Task InitDatabase()
    {
        // Настройка мира
        if (!await _db.Worlds.AnyAsync()) {
            await _worldRepo.AddAsync("Local", "Multiplayer Server", "192.168.31.158", 50052);
        }

        // Создаем 10 тестовых аккаунтов, если их еще нет
        for (int i = 1; i <= 10; i++) {
            string username = $"testuser{i}";
            
            // Проверка, существует ли уже такой пользователь, чтобы избежать ошибок БД
            if (!await _db.Users.AnyAsync(u => u.Username == username)) {
                await _userRepo.AddAsync(username, "password");
                _logger.LogInformation("Account '{username}' registered.", username);
            }
        }
    } // Конец метода InitDatabase

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting Auth server");
        await foreach (var packet in _channel.ReadAllAsync(ct)) {
            await _dispatcher.DispatchAsync(MessageDomain.Auth, packet.Type, packet.Data, packet.Client, ct);
        }
    }
}