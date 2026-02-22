using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;

namespace AISpace.Server;

public class AreaServer(ILogger<AreaServer> logger, MainContext db, IUserRepository userRepo, AreaChannel channel, IWorldRepository worldRepo, PacketDispatcher dispatcher, SharedState state) : DomainServerBase<AreaServer>(logger, db, userRepo, channel.Channel, worldRepo, dispatcher, state)
{
    protected override MessageDomain ActiveDomain => MessageDomain.Area;

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
        // game state update logic goes here
    }
}
