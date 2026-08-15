namespace aisp.Network.Packets.Area;

/// <summary>
/// Client-to-server acknowledgement after a queued event synchronization point
/// (send_event_sync_r). Payload: UInt Result.
/// </summary>
public sealed class EventSyncRRequest(uint result) : IIncomingPacket<EventSyncRRequest>
{
    public uint Result { get; } = result;

    public static EventSyncRRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new EventSyncRRequest(reader.ReadUInt());
    }
}
