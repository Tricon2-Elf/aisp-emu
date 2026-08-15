using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public class FurnitureGetBaseListResponse(uint result, IReadOnlyList<FurnitureBaseEntry> entries)
    : IOutgoingPacket
{
    public const int MaximumEntryCount = 300;

    public FurnitureGetBaseListResponse()
        : this(0, Array.Empty<FurnitureBaseEntry>()) { }

    public uint Result { get; } = result;
    public IReadOnlyList<FurnitureBaseEntry> Entries { get; } = entries;

    public byte[] ToBytes()
    {
        if (Entries.Count > MaximumEntryCount)
            throw new InvalidOperationException(
                $"{nameof(FurnitureGetBaseListResponse)} supports at most {MaximumEntryCount} entries, received {Entries.Count}."
            );

        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write((uint)Entries.Count);
        foreach (var entry in Entries)
        {
            writer.Write(entry.ItemId);
            writer.Write((uint)entry.PlacementFlags);
            writer.Write(entry.Type);
        }

        return writer.ToBytes();
    }
}
