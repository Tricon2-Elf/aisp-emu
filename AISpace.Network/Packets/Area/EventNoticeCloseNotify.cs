namespace AISpace.Network.Packets.Area;

/// <summary>
/// Server-to-client notice dismiss (recv_event_notice_close / 0xF477). Empty payload.
/// </summary>
public sealed class EventNoticeCloseNotify : IOutgoingPacket
{
    public byte[] ToBytes() => [];
}
