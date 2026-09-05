using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_adventure_shop_added_buy_history (0xEEE8, case 0x7F2F8D): one 購入履歴 row (item record + u8 + u32 purchase
/// time, 1594 bytes), appended to the client's history list. Sent after a successful purchase.
/// </summary>
public sealed class AdventureShopAddedBuyHistoryNotify(AdventureShopHistoryRow row)
    : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        row.WriteTo(writer);
        return writer.ToBytes();
    }
}
