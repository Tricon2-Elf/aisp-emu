using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_adventure_shop_item (0x9B08, case 0x7DE878): one page of the drama disc shop lineup. No result field:
/// NUL word (max 0x181 incl. NUL), u32 filter, u32 sort, u32 index (0-based page), u64 search_count (total hits;
/// the client derives the page combo from it, 50 per page), u32 n (max 50), n item records. The client replaces
/// its lineup with the page and selects sort / index in the combos, so both are echoed from the request.
/// </summary>
public sealed class AdventureShopItemNotify(
    string word,
    uint filter,
    uint sort,
    uint index,
    ulong searchCount,
    IReadOnlyList<AdventureShopItemRecord> items
) : IOutgoingPacket
{
    public const int MaxItems = 50;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(word, 384);
        writer.Write(filter);
        writer.Write(sort);
        writer.Write(index);
        writer.Write(searchCount);
        var count = Math.Min(items.Count, MaxItems);
        writer.Write((uint)count);
        for (var i = 0; i < count; i++)
            items[i].WriteTo(writer);
        return writer.ToBytes();
    }
}
