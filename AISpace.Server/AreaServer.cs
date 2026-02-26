using AISpace.Common.DAL.Entities;
using AISpace.Common.Network.Handlers;
using AISpace.Common.Network.Packets;
using AISpace.Common.Game;

namespace AISpace.Server;

public class AreaServer : BackgroundService
{
    private readonly ILogger<AreaServer> _logger;
    private readonly MainContext _db;
    private readonly PacketDispatcher _dispatcher;
    private readonly IUserRepository _userRepo;
    private readonly IWorldRepository _worldRepo;
    private readonly SharedState _state;
    private readonly ChannelReader<Packet> _channel;
    public readonly MessageDomain ActiveDomain = MessageDomain.Area;

    private readonly TimeSpan _tickRate = TimeSpan.FromMilliseconds(1000.0 / 60.0);
    private DateTime _nextTimeUpdate = DateTime.MinValue;

    private static readonly long _serverStartTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public AreaServer(ILogger<AreaServer> logger, MainContext db, IUserRepository userRepo, AreaChannel channel, IWorldRepository worldRepo, PacketDispatcher dispatcher, SharedState state)
    {
        _logger = logger;
        _db = db;
        _channel = channel.Channel;
        _dispatcher = dispatcher;
        _userRepo = userRepo;
        _worldRepo = worldRepo;
        _state = state;

        _db.Database.EnsureCreated();

        if (!db.Items.Any())
        {
            List<Item> items = [];
            _logger.LogInformation("Loading items from CSV");
            if (File.Exists("testitems.csv"))
            {
                foreach (var row in File.ReadLines("testitems.csv"))
                {
                    var parts = row.Split(',');
                    if (parts.Length >= 3)
                        items.Add(new Item { Id = int.Parse(parts[0]), Name = parts[2] });
                }
                items = [.. items.DistinctBy(i => i.Id)];
                db.ChangeTracker.AutoDetectChangesEnabled = false;
                db.Items.AddRange(items);
                db.SaveChanges();
                db.ChangeTracker.AutoDetectChangesEnabled = true;
                _logger.LogInformation("Loaded {count} items", items.Count);
            }
        }
    }

    public static (uint phase, float current, float max) GetServerTime()
    {
        const uint T_EARLY = 900;
        const uint T_MORN  = 1800;
        const uint T_DAY   = 3600;
        const uint T_EVE   = 900;
        const uint T_NIGHT = 1800;
        const uint TOTAL   = 9000;

        long elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _serverStartTime;
        uint cycleTime = (uint)(elapsed % TOTAL);

        if (cycleTime < T_EARLY) return (4, cycleTime, T_EARLY);
        cycleTime -= T_EARLY;
        if (cycleTime < T_MORN) return (0, cycleTime, T_MORN);
        cycleTime -= T_MORN;
        if (cycleTime < T_DAY) return (1, cycleTime, T_DAY);
        cycleTime -= T_DAY;
        if (cycleTime < T_EVE) return (2, cycleTime, T_EVE);
        cycleTime -= T_EVE;
        return (3, cycleTime, T_NIGHT);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting {domain} server", ActiveDomain);
        await Task.WhenAll(RunPacketLoop(ct), RunGameLoop(ct));
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
            UpdateWorld();
        }
    }

    private void UpdateWorld()
    {
        if (DateTime.UtcNow > _nextTimeUpdate)
        {
            var t = TimeZoneService.GetServerTime();
            
            var timePacket = new TimeZoneGetResponse(0, t.phase, t.current, t.max, 0);
            byte[] data = timePacket.ToBytes();

            foreach (var client in _state.AreaClients.Values)
            {
                if (client.IsAuthenticated)
                    _ = client.SendAsync(PacketType.TimeZoneGetResponse, data);
            }

            _nextTimeUpdate = DateTime.UtcNow.AddSeconds(1); 
        }
    }
}
