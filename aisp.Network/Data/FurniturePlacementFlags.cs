namespace aisp.Network.Data;

[Flags]
public enum FurniturePlacementFlags : uint
{
    Floor = 0x08,
    Wall = 0x10,
    Ceiling = 0x20,
}
