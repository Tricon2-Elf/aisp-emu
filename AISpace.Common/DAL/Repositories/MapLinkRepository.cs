using AISpace.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.DAL.Repositories;

public interface IMapLinkRepository
{
    Task<IReadOnlyList<MapLink>> GetBySourceMapAsync(uint sourceMapId, uint channelId, CancellationToken ct = default);
}

public class MapLinkRepository(MainContext db) : IMapLinkRepository
{
    private readonly MainContext _db = db;

    public async Task<IReadOnlyList<MapLink>> GetBySourceMapAsync(uint sourceMapId, uint channelId, CancellationToken ct = default)
    {
        long mapId = sourceMapId;
        long channel = channelId;

        return await _db.MapLinks.AsNoTracking().Where(x => x.IsEnabled && x.SourceMapId == mapId && (x.ChannelId == channel || x.ChannelId == 0)).OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(ct);
    }

    /// <summary>
    /// Corrects the original sample Akihabara direct link so it no longer overlaps the default spawn point.
    /// Applies only to the legacy seeded row shape and leaves user-authored links untouched.
    /// </summary>
    public static async Task NormalizeSeedMapLinksAsync(MainContext db, CancellationToken ct = default)
    {
        var candidate = await db.MapLinks.FirstOrDefaultAsync(x => x.SourceMapId == 10990100 && x.ChannelId == 0 && x.SortOrder == 10 && x.DestinationMapIds == "10990110", ct);

        if (candidate == null)
            return;

        var isLegacyLayout = candidate.PositionY == 2.0f && candidate.PositionZ == -18000f && candidate.Yaw == 0 && (candidate.PositionX == -9100f || candidate.PositionX == -9800f);

        if (!isLegacyLayout)
            return;

        candidate.PositionX = -9800f;
        candidate.PositionY = 2.0f;
        candidate.PositionZ = -18000f;
        candidate.Length = 300f;
        candidate.Depth = 0f;

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Seeds map-link entries if the MapLinks table is empty.</summary>
    public static async Task SeedMapLinksIfEmptyAsync(MainContext db, CancellationToken ct = default)
    {
        if (await db.MapLinks.AnyAsync(ct))
            return;

        var links = new[]
        {
            // Single destination: client can immediately transition (count=1).
            new MapLink
            {
                SourceMapId = 10990100,
                ChannelId = 0,
                PositionX = -8677f,
                PositionY = 2.0f,
                PositionZ = -19312f,
                Yaw = 0,
                Length = 300f,
                Depth = 100f,
                DestinationMapIds = "10990110",
                Behavior = MapLinkBehavior.AutoEnterIfSingle,
                SortOrder = 10,
                IsEnabled = true,
            },
            // Multiple destinations: client should open map selection.
            new MapLink
            {
                SourceMapId = 10990100,
                ChannelId = 0,
                PositionX = -10701f,
                PositionY = 0.1f,
                PositionZ = -19313f,
                Yaw = 0,
                Length = 100f,
                Depth = 10f,
                DestinationMapIds = "10990110,10990200,10990210",
                Behavior = MapLinkBehavior.ForceSelection,
                SortOrder = 20,
                IsEnabled = true,
            },
        };

        db.MapLinks.AddRange(links);
        await db.SaveChangesAsync(ct);
    }
}
