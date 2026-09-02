using aisp.Network;

namespace aisp.Common.DAL.Entities;

public sealed class Furniture
{
    public int ItemId { get; set; }
    public Item Item { get; set; } = default!;
    public uint Type { get; set; }
    public FurniturePlacementFlags PlacementFlags { get; set; }
}
