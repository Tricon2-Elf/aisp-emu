using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_adventure_shop_genre_search_r (0x6DC0, case 0x7D33C3): u32 result. This is what releases the shop
/// window's busy state; the page itself travels in recv_adventure_shop_item, which must be sent first.
/// Non-zero results show the window's error dialog.
/// </summary>
public sealed class AdventureShopGenreSearchResponse(uint result = 0) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        return writer.ToBytes();
    }
}
