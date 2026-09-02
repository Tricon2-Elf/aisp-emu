using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// send_adventure_upload_request_report (0x2494, wrapper 0x7A9210): the client's verdict on the HTTP upload.
/// u32 report (0 = the XML reply said ok), u16 workId, int64 scriptId.
/// </summary>
public sealed class AdventureUploadRequestReportRequest(uint report, ushort workId, long scriptId)
    : IIncomingPacket<AdventureUploadRequestReportRequest>
{
    public uint Report { get; } = report;
    public ushort WorkId { get; } = workId;
    public long ScriptId { get; } = scriptId;

    public static AdventureUploadRequestReportRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var report = reader.ReadUInt();
        var workId = reader.ReadUShort();
        var scriptId = (long)reader.ReadULong();
        return new AdventureUploadRequestReportRequest(report, workId, scriptId);
    }
}
