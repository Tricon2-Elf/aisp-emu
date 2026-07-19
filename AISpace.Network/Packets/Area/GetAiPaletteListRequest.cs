namespace AISpace.Network.Packets.Area;

public sealed class GetAiPaletteListRequest : IIncomingPacket<GetAiPaletteListRequest>
{
    public uint RoboId { get; init; }

    public static GetAiPaletteListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new GetAiPaletteListRequest { RoboId = reader.ReadUInt() };
    }
}
