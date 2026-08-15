using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

/// <summary>
/// Server-to-client island bootstrap packet (recv_select_init_island_start).
/// Decompiled parsing reads:
/// UInt Count + Count * island_t(486 bytes in packet).
/// </summary>
public sealed class SelectInitIslandStartNotify : IOutgoingPacket
{
    public IReadOnlyList<SelectInitIslandEntry> Islands { get; init; } = [];

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)Islands.Count);
        foreach (var island in Islands)
            island.WriteTo(writer);
        return writer.ToBytes();
    }
}
