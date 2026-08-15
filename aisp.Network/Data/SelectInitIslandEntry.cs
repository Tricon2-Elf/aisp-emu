using aisp.Network;

namespace aisp.Network.Data;

/// <summary>
/// One island_t entry for recv_select_init_island_start.
/// Decompiled parsing aligns with:
/// UInt IslandId + FixedString(97, UTF-8) + FixedString(385, UTF-8).
/// </summary>
public sealed class SelectInitIslandEntry
{
    public const int TitleLength = 97;
    public const int DescriptionLength = 385;
    public const int PacketSize = 4 + TitleLength + DescriptionLength;

    public uint IslandId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    public void WriteTo(PacketWriter writer)
    {
        writer.Write(IslandId);
        writer.WriteFixedString(Title, TitleLength);
        writer.WriteFixedString(Description, DescriptionLength);
    }

    public static SelectInitIslandEntry FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new SelectInitIslandEntry
        {
            IslandId = reader.ReadUInt(),
            Title = reader.ReadFixedString(TitleLength),
            Description = reader.ReadFixedString(DescriptionLength),
        };
    }
}
