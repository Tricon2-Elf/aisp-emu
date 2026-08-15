namespace aisp.Network.Packets.Area;

public sealed class GetCosplayListRequest : IIncomingPacket<GetCosplayListRequest>
{
    public uint RoboId { get; init; }

    public static GetCosplayListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new GetCosplayListRequest { RoboId = reader.ReadUInt() };
    }
}
