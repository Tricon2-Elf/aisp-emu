using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>recv_adventure_shop_remove_all_buy_history_r (0xB736, case 0x7E4D1E): u32 result; 0 clears the client's history list.</summary>
public sealed class AdventureShopRemoveAllBuyHistoryResponse(uint result = 0) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        return writer.ToBytes();
    }
}
