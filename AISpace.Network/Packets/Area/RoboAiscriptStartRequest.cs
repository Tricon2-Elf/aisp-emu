namespace AISpace.Network.Packets.Area;

public sealed class RoboAiscriptStartRequest : IIncomingPacket<RoboAiscriptStartRequest>
{
    public uint RoboId { get; init; }

    public static RoboAiscriptStartRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new RoboAiscriptStartRequest { RoboId = reader.ReadUInt() };
    }
}
