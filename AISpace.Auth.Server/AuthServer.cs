using System.Threading.Channels;
using AISpace.Common.DAL;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using Scrutor;

namespace AISpace.Auth.Server;

internal class AuthServer
{
    ILogger<AuthServer> _logger;

    private readonly TcpListenerService<AuthChannel> _listener;
    private readonly MainContext _db;
    private readonly PacketDispatcher _dispatcher;
    private readonly UserRepository _userRepo;
    private readonly WorldRepository _worldRepo;
    private readonly ChannelReader<Packet> _packetChannel;
    public readonly MessageDomain ActiveDomain = MessageDomain.Auth;

    public AuthServer(ILogger<AuthServer> logger, 
        TcpListenerService<AuthChannel> listener, 
        MainContext db, 
        UserRepository userRepo, 
        WorldRepository worldRepo, 
        PacketDispatcher dispatcher)
    {
        _logger = logger;
        _db = db;
        _listener = listener;
        _packetChannel = _listener.PacketReader;
        _dispatcher = dispatcher;
        _userRepo = userRepo;
        _worldRepo = worldRepo;

        //Setup DB
        _db.Database.EnsureCreated();
    }
    public async void Start(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting Auth server");

        _logger.LogInformation("Starting Database connection");
        await _userRepo.AddUserAsync("hideki@animetoshokan.org", "password");
        await _worldRepo.AddWorldAsync("test", "test2");

        
        _logger.LogInformation("Starting TCP Server");
        await _listener.StartAsync(ct);

        _logger.LogInformation("Starting Main Loop");
        await foreach (var packet in _packetChannel.ReadAllAsync(ct))
        {
            await _dispatcher.DispatchAsync(ActiveDomain, packet.Type, packet.Data, packet.Client, ct);
        }
    }


}
