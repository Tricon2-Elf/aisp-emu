using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

/// <summary>
/// Server-to-client area-map selector open event (recv_event_areamap_select_exec).
/// Decompiled parsing reads:
/// UInt Count + Count * select_map_t(109 bytes in packet) + UInt IslandId + UInt IsRegisteredIsland.
/// </summary>
public sealed class EventAreaMapSelectExecNotify : IOutgoingPacket
{
    public IReadOnlyList<NotifySelectMapEntry> Entries { get; init; } = [];
    public uint IslandId { get; init; }
    public uint IsRegisteredIsland { get; init; }
    public IReadOnlyList<uint> MapIds => Entries.Select(entry => entry.MapId).ToList();

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)Entries.Count);
        foreach (var entry in Entries)
            entry.WriteTo(writer);
        writer.Write(IslandId);
        writer.Write(IsRegisteredIsland);
        return writer.ToBytes();
    }
}
