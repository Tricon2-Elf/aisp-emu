namespace aisp.Network.Data;

/// <summary>A ranking board row (parser 0x799DC0): the item record followed by a u16 and a u32, both ignored by the client (rows render like the lineup).</summary>
public sealed record AdventureShopRankingRow(AdventureShopItemRecord Item, ushort Rank, uint Count)
{
    public void WriteTo(PacketWriter writer)
    {
        Item.WriteTo(writer);
        writer.Write(Rank);
        writer.Write(Count);
    }
}

/// <summary>
/// A 購入履歴 row (parser 0x799E50): the item record, a u8 the client ignores, and the purchase time as Unix
/// seconds. The client shows that date, shows purchase + 7 days as the re-download deadline, and refuses to buy
/// the same disc again while a history entry is younger than 7 days. Rows are inserted at the front by the
/// client, so they are sent oldest first.
/// </summary>
public sealed record AdventureShopHistoryRow(
    AdventureShopItemRecord Item,
    byte Flag,
    uint PurchasedAt
)
{
    public void WriteTo(PacketWriter writer)
    {
        Item.WriteTo(writer);
        writer.Write(Flag);
        writer.Write(PurchasedAt);
    }
}
