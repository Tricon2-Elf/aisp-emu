namespace aisp.Network.Data;

/// <summary>
/// A furniture-catalog entry in the client's wire order. The client uses the
/// second word to select the floor, wall, and ceiling placement lists.
/// </summary>
public readonly record struct FurnitureBaseEntry(
    uint ItemId,
    FurniturePlacementFlags PlacementFlags,
    uint Type
);
