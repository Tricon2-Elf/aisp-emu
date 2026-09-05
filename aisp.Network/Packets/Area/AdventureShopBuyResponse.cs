using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_adventure_shop_buy_r (0xFAA8, case 0x7F4C0E): u32 result. Releases the window's wait; 0 shows the purchase
/// completion dialog (unless the download that the server pushed beforehand failed), anything else the error
/// dialog with the code. The client never downloads on its own after this, and the reply carries no ticket.
/// </summary>
public sealed class AdventureShopBuyResponse(uint result = 0) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        return writer.ToBytes();
    }
}
