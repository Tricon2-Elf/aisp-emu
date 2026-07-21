namespace AISpace.Network.Packets.Area;

/// <summary>
/// Client response to recv_event_get_tps_mode. Result is 1 while TPS mode is
/// active and 0 otherwise.
/// </summary>
public sealed class EventGetTpsModeRequest(uint result) : IIncomingPacket<EventGetTpsModeRequest>
{
    public uint Result { get; } = result;

    public static EventGetTpsModeRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new EventGetTpsModeRequest(reader.ReadUInt());
    }
}
