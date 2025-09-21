using System.Threading.Channels;
using AISpace.Common.DAL;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Network;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace AISpace.Auth.Server;

internal class AuthServer
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly TcpListenerService server;
    private readonly PacketDispatcher _dispatcher;
    private readonly UserRepository _userRepo;
    private readonly WorldRepository _worldRepo;
    private readonly MainContext context;
    private readonly ChannelReader<Packet> _packetChannel;
    private readonly MessageDomain domain = MessageDomain.Auth;

    public AuthServer(IServiceProvider services, int port, Channel<Packet> packetChannel)
    {
        _dispatcher = services.GetRequiredService<PacketDispatcher>();
        _packetChannel = packetChannel.Reader;
        //server = new TcpListenerService("0.0.0.0", port, false);

        //Setup DB
        context = new MainContext();
        context.Database.EnsureCreated();
    }
    public async void Start(CancellationToken ct = default)
    {
        _logger.Info("Starting Auth server");

        _logger.Info("Starting Database connection");
        await _userRepo.AddUserAsync("hideki@animetoshokan.org", "password");
        await _worldRepo.AddWorldAsync("test", "test2");
        _logger.Info("Starting TCP Server");
        await server.StartAsync(ct);

        _logger.Info("Starting Main Loop");
        await foreach (var packet in _packetChannel.ReadAllAsync())
        {
            ClientConnection connection = packet.Client;
            string ClientID = packet.Client.Id.ToString();
            var payload = packet.Data;
            var packetType = packet.Type;
            await _dispatcher.DispatchAsync(domain, packetType, payload, connection, ct);

        }
    }


}
