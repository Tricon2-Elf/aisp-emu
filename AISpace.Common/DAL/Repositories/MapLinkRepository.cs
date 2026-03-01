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

        return await _db.MapLinks
            .AsNoTracking()
            .Where(x => x.IsEnabled && x.SourceMapId == mapId && (x.ChannelId == channel || x.ChannelId == 0))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
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
                PositionX = -9100f,
                PositionY = 2.0f,
                PositionZ = -18000f,
                Yaw = 0,
                Length = 1000f,
                Depth = 1000f,
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
                PositionX = -9600f,
                PositionY = 0.1f,
                PositionZ = -8400f,
                Yaw = 0,
                Length = 1000f,
                Depth = 1000f,
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
