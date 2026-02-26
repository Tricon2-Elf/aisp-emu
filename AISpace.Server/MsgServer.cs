using AISpace.Common.Game;
using AISpace.Common.Network.Handlers;
using AISpace.Common.Network.Packets;

namespace AISpace.Server;

public class MsgServer : BackgroundService
{
    private readonly ILogger<MsgServer> _logger;
    private readonly MainContext _db;
    private readonly PacketDispatcher _dispatcher;
    private readonly IUserRepository _userRepo;
    private readonly IWorldRepository _worldRepo;
    private readonly SharedState _state;
    private readonly ChannelReader<Packet> _channel;
    public readonly MessageDomain ActiveDomain = MessageDomain.Msg;

    private readonly TimeSpan _tickRate = TimeSpan.FromMilliseconds(1000.0 / 60.0);

    public MsgServer(ILogger<MsgServer> logger,
        MainContext db,
        IUserRepository userRepo,
        MsgChannel channel,
        IWorldRepository worldRepo,
        PacketDispatcher dispatcher, SharedState state)
    {
        _logger = logger;
        _db = db;
        _channel = channel.Channel;
        _dispatcher = dispatcher;
        _userRepo = userRepo;
        _worldRepo = worldRepo;
        _state = state;

        //Setup DB
        _db.Database.EnsureCreated();
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting {domain} server", ActiveDomain);
        var packetLoop = RunPacketLoop(ct);
        var gameLoop = RunGameLoop(ct);

        await Task.WhenAll(packetLoop, gameLoop);
    }

    private async Task RunPacketLoop(CancellationToken ct)
    {
        await foreach (var packet in _channel.ReadAllAsync(ct))
        {
            await _dispatcher.DispatchAsync(ActiveDomain, packet.Type, packet.Data, packet.Client, ct);
        }
    }

    private async Task RunGameLoop(CancellationToken ct)
    {
        var sw = new PeriodicTimer(_tickRate);
        while (await sw.WaitForNextTickAsync(ct))
        {
            // Process new chat messages
            while (!_state.newMessages.IsEmpty)
            {
                _state.newMessages.TryDequeue(out var message);
                _logger.LogInformation("{id} sent {message}", message.id, message.message);
                //Send message to all other users
            }

        }
    }
}
