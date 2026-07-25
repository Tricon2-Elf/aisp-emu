namespace AISpace.Network.Packets.Area;

public sealed class RoboSquireRequest : IIncomingPacket<RoboSquireRequest>
{
    public uint RoboId { get; init; }

    public static RoboSquireRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new RoboSquireRequest { RoboId = reader.ReadUInt() };
    }
}
