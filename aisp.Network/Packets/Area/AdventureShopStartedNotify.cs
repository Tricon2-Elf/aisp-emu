using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_adventure_shop_started (0x03EA): pushed after the player talks to the drama disc shop's 販売担当 clerk.
/// Unlike recv_adventure_upload_started this is not an NPC id but a catalog snapshot (client parser
/// 0x7BC061, buffer up to 0x294E0 bytes): total count, current keyword, filter/sort/page, hit count, then the
/// first page of listings, the ranking board and the buyer's purchase history. Listings are not modelled yet,
/// so this always sends an empty catalog, which is enough for the window to open.
/// </summary>
public sealed class AdventureShopStartedNotify(
    ulong allCount = 0,
    string word = "",
    uint filter = 0,
    uint sort = 0,
    uint index = 0,
    ulong searchCount = 0,
    uint rankSort = 0
) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(allCount);
        writer.Write(word, 385); // NUL-terminated; the client caps it at 0x181 bytes
        writer.Write(filter);
        writer.Write(sort);
        writer.Write(index);
        writer.Write(searchCount);
        writer.Write(0u); // items[] count (max 50, 1589-byte records)
        writer.Write(rankSort);
        writer.Write(0u); // rankings[] count (max 5, record + u16 + u32)
        writer.Write(0u); // historys[] count (max 50, record + u8 + u32)
        return writer.ToBytes();
    }
}
