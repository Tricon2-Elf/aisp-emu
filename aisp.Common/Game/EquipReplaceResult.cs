using aisp.Network.Data;

namespace aisp.Common.Game;

public sealed record EquippedItemChange(int ItemId, string? ItemName, uint SocketBit);

public sealed record EquipReplaceResult(
    IReadOnlyList<EquippedItemChange> Removed,
    IReadOnlyList<EquippedItemChange> Added,
    IReadOnlyDictionary<int, int> InventoryCountsByItemId
);

public sealed record RoboEquipReplaceResult(RoboData Robo, EquipReplaceResult InventoryChanges);
