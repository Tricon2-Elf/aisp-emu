namespace AISpace.Network.Packets.Area;

/// <summary>
/// Server-to-client dialogue dismiss (recv_event_message_close). Empty payload.
/// </summary>
public sealed class EventMessageCloseNotify : IOutgoingPacket
{
    public byte[] ToBytes() => [];
}
