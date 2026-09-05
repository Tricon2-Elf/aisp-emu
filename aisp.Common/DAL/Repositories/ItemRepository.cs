using System.Text.Json;
using aisp.Common.DAL.Entities;
using aisp.Common.Game;
using aisp.Common.Localisation;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.DAL.Repositories;

public interface IItemRepository
{
    Task<Item?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Item>> GetAllAsync(CancellationToken ct = default);
}

public sealed class ItemRepository(MainContext db) : IItemRepository
{
    private static readonly JsonSerializerOptions JsonOptions = SeedJson.Options;

    public Task<Item?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Items.AsNoTracking().SingleOrDefaultAsync(i => i.Id == id, ct);

    public async Task<IReadOnlyList<Item>> GetAllAsync(CancellationToken ct = default) =>
        await db.Items.AsNoTracking().Include(i => i.Furniture).ToListAsync(ct);

    public static async Task SeedItemsIfEmptyAsync(
        MainContext db,
        string jsonPath,
        CancellationToken ct = default
    )
    {
        if (await db.Items.AnyAsync(ct))
            return;

        if (!File.Exists(jsonPath))
            throw new FileNotFoundException(
                "Item seed JSON not found (required for empty Items table).",
                jsonPath
            );

        var json = await File.ReadAllTextAsync(jsonPath, ct);
        var rows = JsonSerializer.Deserialize<List<ItemSeedRow>>(json, JsonOptions) ?? [];

        var items = new List<Item>(rows.Count);
        foreach (var row in rows.DistinctBy(r => r.Id))
        {
            var canonicalName = row.Name.Canonical;
            items.Add(
                new Item
                {
                    Id = row.Id,
                    Name = canonicalName,
                    Socket = row.Socket,
                    IconId = row.IconId ?? 1,
                    CatalogCategory = (int)
                        ItemEntityMapper.ResolvePersistedCatalogCategory(
                            row.Id,
                            canonicalName,
                            null
                        ),
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

    /// <summary>Adds any seed items that are missing from an existing Items table (idempotent).</summary>
    public static async Task EnsureSeedItemsPresentAsync(
        MainContext db,
        string jsonPath,
        CancellationToken ct = default
    )
    {
        if (!File.Exists(jsonPath))
            return;

        var json = await File.ReadAllTextAsync(jsonPath, ct);
        var rows = JsonSerializer.Deserialize<List<ItemSeedRow>>(json, JsonOptions) ?? [];

        var existingItems = await db.Items.ToListAsync(ct);
        var existingIds = existingItems.Select(item => item.Id).ToHashSet();
        var distinctRows = rows.DistinctBy(r => r.Id).ToList();
        var missing = distinctRows
            .Where(row => !existingIds.Contains(row.Id))
            .Select(row =>
            {
                var canonicalName = row.Name.Canonical;
                return new Item
                {
                    Id = row.Id,
                    Name = canonicalName,
                    Socket = row.Socket,
                    IconId = row.IconId ?? 1,
                    CatalogCategory = (int)
                        ItemEntityMapper.ResolvePersistedCatalogCategory(
                            row.Id,
                            canonicalName,
                            null
                        ),
                };
            })
            .ToList();

        if (missing.Count > 0)
            db.Items.AddRange(missing);

        var rowsById = distinctRows.ToDictionary(row => row.Id);
        foreach (var item in existingItems)
        {
            var resolved = (int)
                ItemEntityMapper.ResolvePersistedCatalogCategory(item.Id, item.Name, null);
            if (item.CatalogCategory is int persisted)
            {
                // Pre-accessory-prefix seeds stored 114xxxxx backpacks as furniture (12-14).
                if (
                    ItemEntityMapper.IsWardrobeAccessoryItem(item.Id)
                    && ItemEntityMapper.IsFurnitureCatalogCategory(persisted)
                )
                    item.CatalogCategory = resolved;
                continue;
            }

            if (!rowsById.ContainsKey(item.Id))
                continue;
            item.CatalogCategory = resolved;
        }

        if (missing.Count == 0 && !db.ChangeTracker.HasChanges())
            return;

        await db.SaveChangesAsync(ct);
    }

    private sealed class ItemSeedRow
    {
        public int Id { get; set; }
        public int Socket { get; set; }
        public LocalisedString Name { get; set; } = new();
        public int? IconId { get; set; }
    }
}
