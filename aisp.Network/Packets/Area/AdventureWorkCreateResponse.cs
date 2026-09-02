using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_adventure_work_create_r (0x7CD2), 10 bytes: UInt Result, UInt Sheets, UShort WorkId.
/// On result 0 the client creates the local work files (新規作成_%03d) under work/drama and registers WorkId in list.csv.
/// </summary>
public sealed class AdventureWorkCreateResponse(uint result, uint sheets, ushort workId)
    : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(sheets);
        writer.Write(workId);
        return writer.ToBytes();
    }
}
