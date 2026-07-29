namespace AISpace.Network.Data;

/// <summary>
/// One entry in recv_shop_item (sub_799AF0): AiPrice + NicoPrice + ItemId.
/// </summary>
public sealed record ShopItemEntry(uint ItemId, ulong AiPrice, ulong NicoPrice);
