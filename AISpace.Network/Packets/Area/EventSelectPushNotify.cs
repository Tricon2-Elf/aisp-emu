using AISpace.Network;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// Server-to-client in-event option append (recv_event_select_push).
/// Payload: null-terminated UTF-8 option label.
/// </summary>
public sealed class EventSelectPushNotify : IOutgoingPacket
{
    public string SelectName { get; init; } = string.Empty;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(SelectName, "utf-8");
        return writer.ToBytes();
    }
}
