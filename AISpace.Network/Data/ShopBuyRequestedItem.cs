namespace AISpace.Network.Data;

/// <summary>
/// One entry in send_shop_buy (sub_797C00): uint + ushort + uint + uint.
/// </summary>
public sealed record ShopBuyRequestedItem(uint ItemId, ushort UnknownWord, uint Unknown1, uint Unknown2);
