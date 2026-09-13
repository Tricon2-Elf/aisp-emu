using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public sealed class NiconiCommonsBaseListResponse(
    uint result,
    IReadOnlyList<NiconiCommonsEntry> entries
) : IOutgoingPacket
{
    public const int MaximumEntryCount = 1024;

    public NiconiCommonsBaseListResponse()
        : this(0, Array.Empty<NiconiCommonsEntry>()) { }

    public uint Result { get; } = result;
    public IReadOnlyList<NiconiCommonsEntry> Entries { get; } = entries;

    public byte[] ToBytes()
    {
        if (Entries.Count > MaximumEntryCount)
            throw new InvalidOperationException(
                $"{nameof(NiconiCommonsBaseListResponse)} supports at most {MaximumEntryCount} entries, received {Entries.Count}."
            );

        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write((uint)Entries.Count);
        foreach (var entry in Entries)
            entry.Write(writer);
        return writer.ToBytes();
    }
}
