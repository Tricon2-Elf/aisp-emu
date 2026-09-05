using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_adventure_shop_ranking_search_r (0x9EA9, case 0x7DEDCF): u32 result, u32 n (max 5 on the board), then
/// n ranking rows (item record + u16 + u32). The client stores the rows but this build never clears its busy
/// flag on it; the ranking tab shows what recv_adventure_shop_started carried.
/// </summary>
public sealed class AdventureShopRankingSearchResponse(
    uint result,
    IReadOnlyList<AdventureShopRankingRow> rankings
) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write((uint)rankings.Count);
        foreach (var row in rankings)
            row.WriteTo(writer);
        return writer.ToBytes();
    }
}
