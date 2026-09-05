using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>recv_adventure_upload_request_report_r (0x1F30), 12 bytes (client case 0x7C2BD8): u32 result, int64 scriptId.</summary>
public sealed class AdventureUploadRequestReportResponse(uint result, long scriptId)
    : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write((ulong)scriptId);
        return writer.ToBytes();
    }
}
