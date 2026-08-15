using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// Server-to-client in-event option list (recv_event_select_exec). Payload: null-terminated UTF-8 text.
/// </summary>
public sealed class EventSelectExecNotify : IOutgoingPacket
{
    public string Text { get; init; } = string.Empty;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Text, "utf-8");
        return writer.ToBytes();
    }
}
