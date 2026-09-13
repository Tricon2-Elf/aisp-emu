using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_niconi_commons_shop_item (0x8C47). Parser 0x7DAF7E: three counted arrays, each a u32
/// count (cap 0x1F4) then count × 16-byte records via 0x79A180 (four u32s, dest stride 0x10).
/// Debug format is niconi_commons[] / ucc_adv_figure[] / ucc_voice[]. Remaining-bytes must be
/// empty or vtable+0x60 drops Area. Success calls vtable+0x374 → 0x4CBF80.
/// </summary>
public sealed class NiconiCommonsShopItemNotify(
    IReadOnlyList<NiconiCommonsShopItemRecord>? commons = null,
    IReadOnlyList<NiconiCommonsShopItemRecord>? figures = null,
    IReadOnlyList<NiconiCommonsShopItemRecord>? voices = null
) : IOutgoingPacket
{
    public const int MaximumEntryCount = 500;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        WriteList(writer, commons);
        WriteList(writer, figures);
        WriteList(writer, voices);
        return writer.ToBytes();
    }

    private static void WriteList(
        PacketWriter writer,
        IReadOnlyList<NiconiCommonsShopItemRecord>? rows
    )
    {
        var count = Math.Min(rows?.Count ?? 0, MaximumEntryCount);
        writer.Write((uint)count);
        for (var i = 0; i < count; i++)
            rows![i].Write(writer);
    }
}
