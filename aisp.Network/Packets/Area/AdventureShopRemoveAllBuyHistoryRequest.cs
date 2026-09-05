using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>send_adventure_shop_remove_all_buy_history (0xB7A0, wrapper 0x7A86E0): empty.</summary>
public sealed class AdventureShopRemoveAllBuyHistoryRequest
    : IIncomingPacket<AdventureShopRemoveAllBuyHistoryRequest>
{
    public static AdventureShopRemoveAllBuyHistoryRequest FromBytes(ReadOnlySpan<byte> data) =>
        new();
}
