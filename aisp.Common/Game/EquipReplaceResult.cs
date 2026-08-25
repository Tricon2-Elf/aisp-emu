using aisp.Network.Data;

namespace aisp.Common.Game;

public sealed record EquippedItemChange(int ItemId, string? ItemName, uint SocketBit);

public sealed record EquipReplaceResult(
    IReadOnlyList<EquippedItemChange> Removed,
    IReadOnlyList<EquippedItemChange> Added,
    IReadOnlyDictionary<int, int> InventoryCountsByItemId,
    IReadOnlyList<uint>? UpdatedRoboIds = null
)
{
    public IReadOnlyList<uint> RoboIdsWithEquipmentChanges => UpdatedRoboIds ?? [];
}

public sealed record RoboEquipReplaceResult(
    RoboData Robo,
    EquipReplaceResult InventoryChanges,
    IReadOnlyList<EquippedItemChange> AvatarRemoved
)
{
    public RoboEquipReplaceResult(RoboData robo, EquipReplaceResult inventoryChanges)
        : this(robo, inventoryChanges, []) { }
}
