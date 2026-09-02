using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_sheet_shop_buy_r (0x92AB, case 0x7DD0D5): u32 result. Carries no stock or balance: the window redraws
/// its remaining-sheet label (and the editor's) from the stock the last recv_adventure_updated_sheet_stack stored,
/// in the tick this reply lands, so the stock push and the money push must precede it. Non-zero shows an error
/// dialog and keeps the window open.
/// </summary>
public sealed class SheetShopBuyResponse(uint result = 0) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        return writer.ToBytes();
    }
}
