using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// Server-to-client in-event select UI init (recv_event_select_init). Payload: UInt SelectType.
/// </summary>
public sealed class EventSelectInitNotify : IOutgoingPacket
{
    public uint SelectType { get; init; }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(SelectType);
        return writer.ToBytes();
    }
}
