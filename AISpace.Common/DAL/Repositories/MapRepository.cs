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

    private static readonly Map[] CanonicalMaps =
    [
        new Map
        {
            MapId = 10010100,
            Name = "D.C. II 1",
            SpawnX = 400f,
            SpawnY = 0.1f,
            SpawnZ = -6400f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10010110,
            Name = "D.C. II 2",
            SpawnX = 400f,
            SpawnY = 0.1f,
            SpawnZ = -6400f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10010200,
            Name = "D.C. II 3",
            SpawnX = 0f,
            SpawnY = 0.1f,
            SpawnZ = 0f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10010210,
            Name = "D.C. II 4",
            SpawnX = 0f,
            SpawnY = 0.1f,
            SpawnZ = 0f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10010300,
            Name = "D.C. II 5",
            SpawnX = 0f,
            SpawnY = 0.1f,
            SpawnZ = 0f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10010310,
            Name = "D.C. II 6",
            SpawnX = 0f,
            SpawnY = 0.1f,
            SpawnZ = 0f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10010400,
            Name = "D.C. II 7",
            SpawnX = 400f,
            SpawnY = 0.1f,
            SpawnZ = -6400f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10020100,
            Name = "CLANNAD 1",
            SpawnX = 22800f,
            SpawnY = 0.1f,
            SpawnZ = -2400f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10020110,
            Name = "CLANNAD 2",
            SpawnX = 22800f,
            SpawnY = 0.1f,
            SpawnZ = -2400f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10020200,
            Name = "CLANNAD 3",
            SpawnX = 0f,
            SpawnY = 0.1f,
            SpawnZ = 0f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10020210,
            Name = "CLANNAD 4",
            SpawnX = 0f,
            SpawnY = 0.1f,
            SpawnZ = 0f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10020300,
            Name = "CLANNAD 5",
            SpawnX = 0f,
            SpawnY = 0.1f,
            SpawnZ = 0f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10020310,
            Name = "CLANNAD 6",
            SpawnX = 0f,
            SpawnY = 0.1f,
            SpawnZ = 0f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10020400,
            Name = "CLANNAD 7",
            SpawnX = 22800f,
            SpawnY = 0.1f,
            SpawnZ = -2400f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10030100,
            Name = "SHUFFLE! 1",
            SpawnX = 10800f,
            SpawnY = 0.1f,
            SpawnZ = -1200f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10030110,
            Name = "SHUFFLE! 2",
            SpawnX = 10800f,
            SpawnY = 0.1f,
            SpawnZ = -1200f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10030200,
            Name = "SHUFFLE! 3",
            SpawnX = 0f,
            SpawnY = 0.1f,
            SpawnZ = 0f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10030210,
            Name = "SHUFFLE! 4",
            SpawnX = 0f,
            SpawnY = 0.1f,
            SpawnZ = 0f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10030300,
            Name = "SHUFFLE! 5",
            SpawnX = 0f,
            SpawnY = 0.1f,
            SpawnZ = 0f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10030310,
            Name = "SHUFFLE! 6",
            SpawnX = 0f,
            SpawnY = 0.1f,
            SpawnZ = 0f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10030400,
            Name = "SHUFFLE! 7",
            SpawnX = 10800f,
            SpawnY = 0.1f,
            SpawnZ = -1200f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10990100,
            Name = "Akihabara",
            SpawnX = -9100f,
            SpawnY = 2f,
            SpawnZ = -18000f,
            SpawnRotation = 90,
        },
        new Map
        {
            MapId = 10990110,
            Name = "Akihabara 2",
            SpawnX = -11000f,
            SpawnY = 0.1f,
            SpawnZ = -19200f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10990200,
            Name = "Akihabara 3",
            SpawnX = -9600f,
            SpawnY = 0.1f,
            SpawnZ = -8400f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10990210,
            Name = "Akihabara 4",
            SpawnX = -9600f,
            SpawnY = 0.1f,
            SpawnZ = -8400f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 20000000,
            Name = "My Room 1",
            SpawnX = 0f,
            SpawnY = 0.1f,
            SpawnZ = 0f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 20000010,
            Name = "My Room 2",
            SpawnX = 0f,
            SpawnY = 0.1f,
            SpawnZ = 0f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 20000020,
            Name = "My Room 3",
            SpawnX = 0f,
            SpawnY = 0.1f,
            SpawnZ = 0f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 20000030,
            Name = "My Room 4",
            SpawnX = 0f,
            SpawnY = 0.1f,
            SpawnZ = 0f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10900100,
            Name = "Avatar Make",
            SpawnX = 0f,
            SpawnY = 0.1f,
            SpawnZ = 0f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10990400,
            Name = "TPS Lobby",
            SpawnX = 0f,
            SpawnY = 0.1f,
            SpawnZ = 0f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 40990200,
            Name = "TPS UDX",
            SpawnX = 0f,
            SpawnY = 0.1f,
            SpawnZ = 0f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 40010100,
            Name = "TPS Kazami",
            SpawnX = 0f,
            SpawnY = 0.1f,
            SpawnZ = 0f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 40020100,
            Name = "TPS Mitsuzaka",
            SpawnX = 0f,
            SpawnY = 0.1f,
            SpawnZ = 0f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 40030100,
            Name = "TPS Verbena",
            SpawnX = 0f,
            SpawnY = 0.1f,
            SpawnZ = 0f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10040100,
            Name = "Touhou",
            SpawnX = 0f,
            SpawnY = 0.1f,
            SpawnZ = 0f,
            SpawnRotation = 0,
        },
        new Map
        {
            MapId = 10050100,
            Name = "Koihime",
            SpawnX = 0f,
            SpawnY = 0.1f,
            SpawnZ = 0f,
            SpawnRotation = 0,
        },
    ];

    public async Task<Map?> GetByMapIdAsync(uint mapId, CancellationToken ct = default)
    {
        long id = mapId;
        return await _db.Maps.AsNoTracking().FirstOrDefaultAsync(m => m.MapId == id, ct);
    }

    public static async Task EnsureSeedMapsPresentAsync(MainContext db, CancellationToken ct = default)
    {
        var existingMapIds = await db.Maps.Select(map => map.MapId).ToListAsync(ct);
        var existingSet = existingMapIds.ToHashSet();

        var missingMaps = CanonicalMaps
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
    public static async Task SeedMapsIfEmptyAsync(MainContext db, CancellationToken ct = default)
    {
        if (await db.Maps.AnyAsync(ct))
            return;
        db.Maps.AddRange(
            CanonicalMaps.Select(map => new Map
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
}
