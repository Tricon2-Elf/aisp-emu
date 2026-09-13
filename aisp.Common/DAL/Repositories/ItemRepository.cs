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

    /// <summary>Adds missing seed items and upgrades placeholder metadata (idempotent).</summary>
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
            if (
                rowsById.TryGetValue(item.Id, out var identified)
                && !string.IsNullOrWhiteSpace(identified.Name.Canonical)
                && identified.Name.Canonical != "N/A"
            )
            {
                if (item.Name == "N/A")
                {
                    var previousCategory = (int)
                        ItemEntityMapper.ResolvePersistedCatalogCategory(item.Id, item.Name, null);
                    item.Name = identified.Name.Canonical;
                    if (item.CatalogCategory == previousCategory)
                        item.CatalogCategory = (int)
                            ItemEntityMapper.ResolvePersistedCatalogCategory(
                                item.Id,
                                item.Name,
                                null
                            );
                }
                if (item.Socket == 0)
                    item.Socket = identified.Socket;
            }
            var resolved = (int)
                ItemEntityMapper.ResolvePersistedCatalogCategory(item.Id, item.Name, null);
            if (item.CatalogCategory is int persisted)
            {
                // Older seeds filed backpacks, figure boxes, and the second clothing
                // range as furniture, the fallback for every prefix from 110 up.
                if (
                    (
                        ItemEntityMapper.IsWardrobeAccessoryItem(item.Id)
                        || ItemEntityMapper.IsDramaFigureItem(item.Id)
                        || ItemEntityMapper.IsCosplayWardrobeItem(item.Id)
                    ) && ItemEntityMapper.IsFurnitureCatalogCategory(persisted)
                )
                    item.CatalogCategory = resolved;
                continue;
            }

            if (!rowsById.ContainsKey(item.Id))
                continue;
            item.CatalogCategory = resolved;
        }

        var namesByKey = distinctRows.ToDictionary(
            row => L.Item.Name(row.Id).Value,
            row => row.Name
        );
        var placeholderNames = await db
            .LocalisedTexts.Where(text => text.Value == "N/A")
            .ToListAsync(ct);
        foreach (var text in placeholderNames)
        {
            if (
                namesByKey.TryGetValue(text.Key, out var name)
                && name[text.Language] is { } replacement
                && !string.IsNullOrWhiteSpace(replacement)
                && replacement != "N/A"
            )
                text.Value = replacement;
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
