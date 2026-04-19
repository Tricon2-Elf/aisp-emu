using AISpace.Common;
using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Server;

public class AreaServer(ILogger<AreaServer> logger, MainContext db, IUserRepository userRepo, int port, ILoggerFactory loggerFactory, IWorldRepository worldRepo, PacketDispatcher dispatcher, SharedState state, DomainServerHealthRegistry healthRegistry)
    : DomainServerBase<AreaServer>(logger, db, userRepo, port, "Area", loggerFactory, worldRepo, dispatcher, state, healthRegistry, DomainServerHealthRegistry.Keys.AreaServer)
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

    private void UpdateWorld()
    {
        if (DateTime.UtcNow > _nextTimeUpdate)
        {
            var t = TimeZoneService.GetServerTime();

            var timePacket = new TimeZoneGetResponse(0, (uint)t.Phase, t.Current, t.Max, 0);
            byte[] data = timePacket.ToBytes();

            foreach (var client in State.AreaClients.Values)
            {
                if (client.IsAuthenticated)
                    _ = client.SendAsync(PacketType.TimeZoneGetResponse, data);
            }

            _nextTimeUpdate = DateTime.UtcNow.AddSeconds(1);
        }
    }
}
