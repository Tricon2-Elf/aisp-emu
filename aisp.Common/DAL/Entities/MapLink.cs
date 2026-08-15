namespace aisp.Common.DAL.Entities;

public enum MapLinkBehavior
{
    AutoEnterIfSingle = 0,
    ForceSelection = 1,
}

/// <summary>
/// A map link interaction entry for a source map/channel.
/// DestinationMapIds stores 1-4 destination map ids as comma-separated uints.
/// Optional DestinationSpawn* overrides the destination map's default spawn for 1:1 links.
/// </summary>
public class MapLink
{
    public int Id { get; set; }
    public long SourceMapId { get; set; }
    public long ChannelId { get; set; }

    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }

    /// <summary>Trigger orientation in degrees.</summary>
    public int Yaw { get; set; }

    public float Length { get; set; }
    public float Depth { get; set; }

    public string DestinationMapIds { get; set; } = string.Empty;

    /// <summary>Arrival X on the destination map. When all DestinationSpawn* are set, overrides <see cref="Map.SpawnX"/>.</summary>
    public float? DestinationSpawnX { get; set; }

    /// <summary>Arrival Y on the destination map.</summary>
    public float? DestinationSpawnY { get; set; }

    /// <summary>Arrival Z on the destination map.</summary>
    public float? DestinationSpawnZ { get; set; }

    /// <summary>Arrival rotation on the destination map.</summary>
    public int? DestinationSpawnRotation { get; set; }

    public MapLinkBehavior Behavior { get; set; } = MapLinkBehavior.AutoEnterIfSingle;
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;

    public IReadOnlyList<uint> ParseDestinationMapIds()
    {
        if (string.IsNullOrWhiteSpace(DestinationMapIds))
            return [];

        var parsed = DestinationMapIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(raw => uint.TryParse(raw, out var mapId) ? mapId : 0u)
            .Where(mapId => mapId != 0)
            .Distinct()
            .Take(4)
            .ToList();

        return parsed;
    }

    /// <summary>
    /// Returns the per-link destination spawn when all four fields are set; otherwise falls back to the destination map spawn.
    /// </summary>
    public (float X, float Y, float Z, int Rotation) ResolveDestinationSpawn(Map destinationMap)
    {
        if (
            DestinationSpawnX is { } x
            && DestinationSpawnY is { } y
            && DestinationSpawnZ is { } z
            && DestinationSpawnRotation is { } rotation
        )
            return (x, y, z, rotation);

        return (
            destinationMap.SpawnX,
            destinationMap.SpawnY,
            destinationMap.SpawnZ,
            destinationMap.SpawnRotation
        );
    }
}
