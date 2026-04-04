using AISpace.Network;

namespace AISpace.Network.Packets.Area;

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

    public static EventAreaMapSelectExecNotify FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var count = reader.ReadUInt();
        if (count > 4)
            throw new InvalidDataException($"Area map selection count {count} exceeds client maximum of 4.");

        var entries = new List<NotifySelectMapEntry>((int)count);
        for (var index = 0; index < count; index++)
            entries.Add(NotifySelectMapEntry.FromBytes(reader.ReadBytes(NotifySelectMapEntry.PacketSize)));

        return new EventAreaMapSelectExecNotify
        {
            Entries = entries,
            IslandId = reader.ReadUInt(),
            IsRegisteredIsland = reader.ReadUInt(),
        };
    }
}
