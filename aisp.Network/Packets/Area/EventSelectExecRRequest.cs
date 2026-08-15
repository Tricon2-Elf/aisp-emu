namespace aisp.Network.Packets.Area;

/// <summary>
/// Client-to-server in-event option selection reply (send_event_select_exec_r).
/// Payload: UInt Result + Byte SelectNo.
/// </summary>
public sealed class EventSelectExecRRequest : IIncomingPacket<EventSelectExecRRequest>
{
    public uint Result { get; init; }
    public byte SelectNo { get; init; }

    public static EventSelectExecRRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new EventSelectExecRRequest
        {
            Result = reader.ReadUInt(),
            SelectNo = reader.ReadByte(),
        };
    }
}
