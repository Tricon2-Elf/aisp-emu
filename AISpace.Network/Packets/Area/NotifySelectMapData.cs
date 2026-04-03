using AISpace.Network;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// Server-to-client notify select map (recv_notify_select_map). Tells the client which map each maplink leads to.
/// Order of entries must match the order of maplinks sent via MapLinkNotifyData.
/// Payload: UInt Count (1-4) + Count × select_map_t (109 bytes each in packet; first 4 bytes = MapId).
/// </summary>
public class NotifySelectMapData : IOutgoingPacket
{
    /// <summary>Map IDs to which maplinks lead, in the same order as the maplinks.</summary>
    public IReadOnlyList<uint> MapIds { get; set; } = [];

    public NotifySelectMapData() { }

    public NotifySelectMapData(uint singleMapId)
    {
        MapIds = [singleMapId];
    }

    public NotifySelectMapData(IEnumerable<uint> mapIds)
    {
        MapIds = mapIds.ToList();
    }

    public byte[] ToBytes()
    {
        // sub_7987D0 reads per entry: 4 (mapId) + 97 bytes + 4 + 4 = 109 bytes (p_port+=28 is output struct stride, not packet)
        const int SelectMapEntrySizeInPacket = 109;
        var writer = new PacketWriter();
        writer.Write((uint)MapIds.Count);
        Span<byte> padding = stackalloc byte[SelectMapEntrySizeInPacket - 4];
        padding.Clear();
        foreach (var mapId in MapIds)
        {
            writer.Write(mapId);
            writer.Write(padding);
        }
        return writer.ToBytes();
    }
}
