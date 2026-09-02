using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_get_adventure_work_list_r (0xEA66): UInt Result, UInt SheetStock (原稿用紙 the account holds), UInt Count (max 100),
/// then Count packed 13-byte records: UInt WorkId, UInt Sheets, UInt Reserved, Byte Uploaded. The client merges the
/// records by WorkId with its local work/drama/list.csv to build the 自作ドラマ list.
/// </summary>
public sealed class GetAdventureWorkListResponse(
    uint result = 0,
    uint sheetStock = 0,
    IReadOnlyList<(uint WorkId, uint Sheets, bool Uploaded)>? works = null
) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var list = works ?? [];
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(sheetStock);
        writer.Write((uint)list.Count);
        foreach (var (workId, sheets, uploaded) in list)
        {
            writer.Write(workId);
            writer.Write(sheets);
            writer.Write(0u);
            writer.Write((byte)(uploaded ? 1 : 0));
        }
        return writer.ToBytes();
    }
}
