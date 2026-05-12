using System.Text.Json;
using AISpace.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.DAL.Repositories;

public interface IMapRepository
{
    Task<Map?> GetByMapIdAsync(uint mapId, CancellationToken ct = default);
}

public class MapRepository(MainContext db) : IMapRepository
{
    private readonly MainContext _db = db;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<Map?> GetByMapIdAsync(uint mapId, CancellationToken ct = default)
    {
        long id = mapId;
        return await _db.Maps.AsNoTracking().FirstOrDefaultAsync(m => m.MapId == id, ct);
    }

    public static async Task EnsureSeedMapsPresentAsync(MainContext db, string jsonPath, CancellationToken ct = default)
    {
        var canonicalMaps = await LoadMapsFromJsonAsync(jsonPath, ct);
        var existingMapIds = await db.Maps.Select(map => map.MapId).ToListAsync(ct);
        var existingSet = existingMapIds.ToHashSet();

        var missingMaps = canonicalMaps
            .Where(map => !existingSet.Contains(map.MapId))
            .Select(map => new Map
            {
                MapId = map.MapId,
                Name = map.Name,
                SpawnX = map.SpawnX,
                SpawnY = map.SpawnY,
                SpawnZ = map.SpawnZ,
                SpawnRotation = map.SpawnRotation,
            })
            .ToList();

        if (missingMaps.Count == 0)
            return;

        db.Maps.AddRange(missingMaps);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Seeds map data if the Maps table is empty. Call on startup after EnsureCreated.</summary>
    public static async Task SeedMapsIfEmptyAsync(MainContext db, string jsonPath, CancellationToken ct = default)
    {
        if (await db.Maps.AnyAsync(ct))
            return;

        var canonicalMaps = await LoadMapsFromJsonAsync(jsonPath, ct);
        db.Maps.AddRange(
            canonicalMaps.Select(map => new Map
            {
                MapId = map.MapId,
                Name = map.Name,
                SpawnX = map.SpawnX,
                SpawnY = map.SpawnY,
                SpawnZ = map.SpawnZ,
                SpawnRotation = map.SpawnRotation,
            })
        );
        await db.SaveChangesAsync(ct);
    }

    private static async Task<List<Map>> LoadMapsFromJsonAsync(string jsonPath, CancellationToken ct)
    {
        if (!File.Exists(jsonPath))
            throw new FileNotFoundException("Map seed JSON not found.", jsonPath);

        var json = await File.ReadAllTextAsync(jsonPath, ct);
        var rows = JsonSerializer.Deserialize<List<MapSeedRow>>(json, JsonOptions) ?? [];

        return rows
            .DistinctBy(r => r.MapId)
            .Select(r => new Map
            {
                MapId = r.MapId,
                Name = r.Name,
                SpawnX = r.SpawnX,
                SpawnY = r.SpawnY,
                SpawnZ = r.SpawnZ,
                SpawnRotation = r.SpawnRotation,
            })
            .ToList();
    }

    private sealed class MapSeedRow
    {
        public long MapId { get; set; }
        public string Name { get; set; } = "";
        public float SpawnX { get; set; }
        public float SpawnY { get; set; }
        public float SpawnZ { get; set; }
        public int SpawnRotation { get; set; }
    }
}
