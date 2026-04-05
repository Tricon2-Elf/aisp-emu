namespace AISpace.Common.Game;

public sealed record AreaMapSelectionDestination(uint MapId, uint ChannelId);

public sealed class PendingAreaMapSelection
{
    public int LinkId { get; init; }
    public uint SourceMapId { get; init; }
    public uint ChannelId { get; init; }
    public uint IslandId { get; init; }
    public uint IsRegisteredIsland { get; init; }
    public IReadOnlyList<AreaMapSelectionDestination> Destinations { get; init; } = [];
    public bool AwaitingIslandBootstrapAck { get; set; } = true;
    public bool SelectorOpened { get; set; }
}
