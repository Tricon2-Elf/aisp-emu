namespace AISpace.Network.Packets.Area;

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

    public static SelectInitIslandStartNotify FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var count = reader.ReadUInt();
        if (count > 5)
            throw new InvalidDataException($"Island bootstrap count {count} exceeds client maximum of 5.");

        var islands = new List<SelectInitIslandEntry>((int)count);
        for (var index = 0; index < count; index++)
            islands.Add(SelectInitIslandEntry.FromBytes(reader.ReadBytes(SelectInitIslandEntry.PacketSize)));

        return new SelectInitIslandStartNotify { Islands = islands };
    }
}
