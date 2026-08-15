namespace aisp.Network.Packets.Area;

/// <summary>
/// Server-to-client event synchronization point (recv_event_sync). Empty payload.
/// The client replies with <see cref="EventSyncRRequest"/> after earlier queued event
/// actions have completed.
/// </summary>
public sealed class EventSyncNotify : IOutgoingPacket
{
    public byte[] ToBytes() => [];
}
