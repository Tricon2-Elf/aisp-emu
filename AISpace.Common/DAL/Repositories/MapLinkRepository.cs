using System.Text.Json;
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

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<MapLink>> GetBySourceMapAsync(uint sourceMapId, uint channelId, CancellationToken ct = default)
    {
        long mapId = sourceMapId;
        long channel = channelId;

        return await _db.MapLinks.AsNoTracking().Where(x => x.IsEnabled && x.SourceMapId == mapId && (x.ChannelId == channel || x.ChannelId == 0)).OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(ct);
    }

    /// <summary>Replaces all map-link entries from seed JSON on every call.</summary>
    public static async Task SeedMapLinksIfEmptyAsync(MainContext db, string jsonPath, CancellationToken ct = default)
    {
        await db.MapLinks.ExecuteDeleteAsync(ct);

        if (!File.Exists(jsonPath))
            throw new FileNotFoundException("Map link seed JSON not found.", jsonPath);

        var json = await File.ReadAllTextAsync(jsonPath, ct);
        var rows = JsonSerializer.Deserialize<List<MapLinkSeedRow>>(json, JsonOptions) ?? [];

        var links = rows.Select(r => new MapLink
            {
                SourceMapId = r.SourceMapId,
                ChannelId = r.ChannelId,
                PositionX = r.PositionX,
                PositionY = r.PositionY,
                PositionZ = r.PositionZ,
                Yaw = r.Yaw,
                Length = r.Length,
                Depth = r.Depth,
                DestinationMapIds = r.DestinationMapIds ?? "",
                Behavior = r.Behavior,
                SortOrder = r.SortOrder,
                IsEnabled = r.IsEnabled,
            })
            .ToList();

        db.MapLinks.AddRange(links);
        await db.SaveChangesAsync(ct);
    }

    private sealed class MapLinkSeedRow
    {
        public long SourceMapId { get; set; }
        public long ChannelId { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public byte Yaw { get; set; }
        public float Length { get; set; }
        public float Depth { get; set; }
        public string? DestinationMapIds { get; set; }
        public MapLinkBehavior Behavior { get; set; }
        public int SortOrder { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}
