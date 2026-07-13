using AISpace.Network;
using AISpace.Network.Data;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// Server-to-client in-event island picker (recv_event_island_select_exec).
/// Decompiled parsing reads UInt Count + Count * (island_t + UInt), max 5 islands.
/// </summary>
public sealed class EventIslandSelectExecNotify : IOutgoingPacket
{
    public IReadOnlyList<EventIslandSelectEntry> Islands { get; init; } = [];

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)Islands.Count);
        foreach (var island in Islands)
            island.WriteTo(writer);
        return writer.ToBytes();
    }
}
