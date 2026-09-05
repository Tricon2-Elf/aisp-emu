using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_adventure_work_add_sheet_r (0xCEF4) / recv_adventure_work_sub_sheet_r (0x216E), 10 bytes: UInt Result, UShort WorkId, UInt Delta. The client adds Delta to its local sheet count for WorkId (it does not replace it).
/// </summary>
public sealed class AdventureWorkSheetResponse(uint result, ushort workId, uint delta)
    : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(workId);
        writer.Write(delta);
        return writer.ToBytes();
    }
}
