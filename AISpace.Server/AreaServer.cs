using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;

namespace AISpace.Server;

public class AreaServer(ILogger<AreaServer> logger, MainContext db, IUserRepository userRepo, AreaChannel channel, IWorldRepository worldRepo, PacketDispatcher dispatcher, SharedState state) : DomainServerBase<AreaServer>(logger, db, userRepo, channel.Channel, worldRepo, dispatcher, state)
{
    protected override MessageDomain ActiveDomain => MessageDomain.Area;
    private static readonly long _serverStartTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    private DateTime _nextTimeUpdate = DateTime.MinValue;

    protected override void Initialize()
    {
        if (Db.Items.Any())
            return;

        List<Item> items = [];
        Logger.LogInformation("Loading items from CSV");
        foreach (var row in File.ReadLines("testitems.csv"))
            items.Add(new Item { Id = int.Parse(row.Split(',')[0]), Name = row.Split(',')[2] });

        items = [.. items.DistinctBy(i => i.Id)];

        Db.ChangeTracker.AutoDetectChangesEnabled = false;
        Db.Items.AddRange(items);
        Db.SaveChanges();
        Db.ChangeTracker.AutoDetectChangesEnabled = true;
        Logger.LogInformation("Loaded {count} items", items.Count);
    }

    protected override void OnTick(CancellationToken ct) => UpdateWorld();

    public static (uint phase, float current, float max) GetServerTime()
    {
        const uint T_EARLY = 900;
        const uint T_MORN = 1800;
        const uint T_DAY = 3600;
        const uint T_EVE = 900;
        const uint T_NIGHT = 1800;
        const uint TOTAL = 9000;

        long elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _serverStartTime;
        uint cycleTime = (uint)(elapsed % TOTAL);

        if (cycleTime < T_EARLY)
            return (4, cycleTime, T_EARLY);
        cycleTime -= T_EARLY;
        if (cycleTime < T_MORN)
            return (0, cycleTime, T_MORN);
        cycleTime -= T_MORN;
        if (cycleTime < T_DAY)
            return (1, cycleTime, T_DAY);
        cycleTime -= T_DAY;
        if (cycleTime < T_EVE)
            return (2, cycleTime, T_EVE);
        cycleTime -= T_EVE;
        return (3, cycleTime, T_NIGHT);
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
