namespace aisp.Network.Packets.Area;

public sealed class RoboCallRequest : IIncomingPacket<RoboCallRequest>
{
    public uint RoboId { get; init; }

    public static RoboCallRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new RoboCallRequest { RoboId = reader.ReadUInt() };
    }
}
