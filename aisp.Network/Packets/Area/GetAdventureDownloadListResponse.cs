using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_get_adventure_download_list_r (0xA39A, case 0x7DFF50): u32 result, u32 count (max 1000), then count packed
/// 17-byte records (<see cref="AdventureDownloadListRecord"/>): the discs the account holds copies of.
/// </summary>
public sealed class GetAdventureDownloadListResponse(
    uint result = 0,
    IReadOnlyList<AdventureDownloadListRecord>? records = null
) : IOutgoingPacket
{
    public const int MaxRecords = 1000;

    public byte[] ToBytes()
    {
        var list = records ?? [];
        var count = Math.Min(list.Count, MaxRecords);
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write((uint)count);
        for (var i = 0; i < count; i++)
            list[i].WriteTo(writer);
        return writer.ToBytes();
    }
}
