namespace AISpace.Network.Packets.Area;

public sealed class SelectInitIslandEndRequest
{
    public uint IslandId { get; init; }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(IslandId);
        return writer.ToBytes();
    }

    public static SelectInitIslandEndRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new SelectInitIslandEndRequest { IslandId = reader.ReadUInt() };
    }
}
