using aisp.Network;

namespace aisp.Network.Packets.Area;

public sealed class SelectInitIslandEndRequest : IIncomingPacket<SelectInitIslandEndRequest>
{
    public uint IslandId { get; init; }

    public static SelectInitIslandEndRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new SelectInitIslandEndRequest { IslandId = reader.ReadUInt() };
    }
}
