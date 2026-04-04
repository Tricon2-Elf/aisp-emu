using AISpace.Network;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// Server-to-client notify select map (recv_notify_select_map). Tells the client which map each maplink leads to.
/// Order of entries must match the order of maplinks sent via MapLinkNotifyData.
/// Payload: UInt Count (1-4) + Count × select_map_t (109 bytes each in packet).
/// </summary>
public class NotifySelectMapData : IOutgoingPacket
{
    public IReadOnlyList<NotifySelectMapEntry> Entries { get; set; } = [];

    public NotifySelectMapData() { }

    public NotifySelectMapData(NotifySelectMapEntry singleEntry)
    {
        Entries = [singleEntry];
    }

    public NotifySelectMapData(IEnumerable<NotifySelectMapEntry> entries)
    {
        Entries = entries.ToList();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)Entries.Count);
        foreach (var entry in Entries)
            entry.WriteTo(writer);
        return writer.ToBytes();
    }
}
