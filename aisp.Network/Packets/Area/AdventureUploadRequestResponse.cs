using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_adventure_upload_request_r (0xF857), fixed 55 bytes (client case 0x7F41D1): u32 result, u16 workId,
/// int64 scriptId, char[41] one-time ticket for the HTTP upload. On result 0 the client POSTs the manuscript to
/// upload.php with userid / scriptid / ticket and then reports the outcome with send_adventure_upload_request_report.
/// </summary>
public sealed class AdventureUploadRequestResponse(
    uint result,
    ushort workId,
    long scriptId = 0,
    string ticket = ""
) : IOutgoingPacket
{
    public const int TicketLength = 41;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(workId);
        writer.Write((ulong)scriptId);
        writer.WriteFixedString(ticket, TicketLength);
        return writer.ToBytes();
    }
}
