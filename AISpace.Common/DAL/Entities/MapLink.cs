namespace AISpace.Common.DAL.Entities;

public enum MapLinkBehavior
{
    AutoEnterIfSingle = 0,
    ForceSelection = 1,
}

/// <summary>
/// A map link interaction entry for a source map/channel.
/// DestinationMapIds stores 1-4 destination map ids as comma-separated uints.
/// </summary>
public class MapLink
{
    public int Id { get; set; }
    public long SourceMapId { get; set; }
    public long ChannelId { get; set; }

    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }
    public byte Yaw { get; set; }
    public float Length { get; set; }
    public float Depth { get; set; }

    public string DestinationMapIds { get; set; } = string.Empty;
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
}
