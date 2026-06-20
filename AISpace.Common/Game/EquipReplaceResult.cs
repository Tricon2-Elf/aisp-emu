namespace AISpace.Common.Game;

public sealed record EquippedItemChange(int ItemId, string? ItemName, uint SocketBit);

public sealed record EquipReplaceResult(
    IReadOnlyList<EquippedItemChange> Removed,
    IReadOnlyList<EquippedItemChange> Added,
    IReadOnlyDictionary<int, int> InventoryCountsByItemId
);
