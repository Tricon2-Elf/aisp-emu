using System.Text.Json;
using AISpace.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.DAL.Repositories;

public interface IItemRepository
{
    Task<Item?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Item>> GetAllAsync(CancellationToken ct = default);
}

public sealed class ItemRepository(MainContext db) : IItemRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public Task<Item?> GetByIdAsync(int id, CancellationToken ct = default) => db.Items.AsNoTracking().SingleOrDefaultAsync(i => i.Id == id, ct);

    public async Task<IReadOnlyList<Item>> GetAllAsync(CancellationToken ct = default) => await db.Items.AsNoTracking().ToListAsync(ct);
    
    public static async Task SeedItemsIfEmptyAsync(MainContext db, string jsonPath, CancellationToken ct = default)
    {
        if (await db.Items.AnyAsync(ct))
            return;

        if (!File.Exists(jsonPath))
            throw new FileNotFoundException("Item seed JSON not found (required for empty Items table).", jsonPath);

        var json = await File.ReadAllTextAsync(jsonPath, ct);
        var rows = JsonSerializer.Deserialize<List<ItemSeedRow>>(json, JsonOptions) ?? [];

        var items = new List<Item>(rows.Count);
        foreach (var row in rows.DistinctBy(r => r.Id))
        {
            items.Add(
                new Item
                {
                    Id = row.Id,
                    Name = row.Name,
                    Socket = row.Socket,
                    IconId = row.IconId ?? 1,
                }
            );
        }

        db.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            db.Items.AddRange(items);
            await db.SaveChangesAsync(ct);
        }
        finally
        {
            db.ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }

    private sealed class ItemSeedRow
    {
        public int Id { get; set; }
        public int Socket { get; set; }
        public string Name { get; set; } = "";
        public int? IconId { get; set; }
    }
}
