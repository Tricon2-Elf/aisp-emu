using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_adventure_shop_started (0x03EA): pushed after the player talks to the drama disc shop's 販売担当 clerk.
/// Unlike recv_adventure_upload_started this is not an NPC id but a catalog snapshot (client parser
/// 0x7BC061, buffer up to 0x294E0 bytes): total count, current keyword, filter/sort/page, hit count, the first
/// page of listings (max 50), the ranking board (max 5) and the buyer's purchase history (max 50). The window
/// sends nothing on open, so this is the only source of its initial lineup, ranking tab and 購入履歴.
/// </summary>
public sealed class AdventureShopStartedNotify(
    ulong allCount = 0,
    string word = "",
    uint filter = 0,
    uint sort = 0,
    uint index = 0,
    ulong searchCount = 0,
    IReadOnlyList<AdventureShopItemRecord>? items = null,
    uint rankSort = 0,
    IReadOnlyList<AdventureShopRankingRow>? rankings = null,
    IReadOnlyList<AdventureShopHistoryRow>? historys = null
) : IOutgoingPacket
{
    public const int MaxItems = 50;
    public const int MaxRankings = 5;
    public const int MaxHistorys = 50;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(allCount);
        writer.Write(word, 384); // NUL-terminated; the client caps it at 0x181 bytes including the NUL
        writer.Write(filter);
        writer.Write(sort);
        writer.Write(index);
        writer.Write(searchCount);
        WriteList(writer, items, MaxItems, (w, r) => r.WriteTo(w));
        writer.Write(rankSort);
        WriteList(writer, rankings, MaxRankings, (w, r) => r.WriteTo(w));
        WriteList(writer, historys, MaxHistorys, (w, r) => r.WriteTo(w));
        return writer.ToBytes();
    }

    private static void WriteList<T>(
        PacketWriter writer,
        IReadOnlyList<T>? rows,
        int max,
        Action<PacketWriter, T> write
    )
    {
        var count = Math.Min(rows?.Count ?? 0, max);
        writer.Write((uint)count);
        for (var i = 0; i < count; i++)
            write(writer, rows![i]);
    }
}
