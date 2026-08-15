using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Data;

/// <summary>
/// One island entry for recv_event_island_select_exec.
/// Decompiled parsing uses sub_798790: island_t (486 bytes) + UInt extra field.
/// </summary>
public sealed class EventIslandSelectEntry
{
    public const int PacketSize = SelectInitIslandEntry.PacketSize + 4;

    public uint IslandId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public uint Extra { get; init; }

    public void WriteTo(PacketWriter writer)
    {
        new SelectInitIslandEntry
        {
            IslandId = IslandId,
            Title = Title,
            Description = Description,
        }.WriteTo(writer);
        writer.Write(Extra);
    }

    public static EventIslandSelectEntry FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var baseEntry = SelectInitIslandEntry.FromBytes(
            reader.ReadBytes(SelectInitIslandEntry.PacketSize)
        );
        return new EventIslandSelectEntry
        {
            IslandId = baseEntry.IslandId,
            Title = baseEntry.Title,
            Description = baseEntry.Description,
            Extra = reader.ReadUInt(),
        };
    }
}
