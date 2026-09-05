using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_get_adventure_upload_list_r (0x49B5, case 0x7CD994): u32 result, u32 count (max 100), then count packed
/// 1574-byte records (<see cref="AdventureUploadListRecord"/>). The account's discs currently on sale, shown in
/// the upload window's right-hand list.
/// </summary>
public sealed class GetAdventureUploadListResponse(
    uint result = 0,
    IReadOnlyList<AdventureUploadListRecord>? records = null
) : IOutgoingPacket
{
    public const int MaxRecords = 100;

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
