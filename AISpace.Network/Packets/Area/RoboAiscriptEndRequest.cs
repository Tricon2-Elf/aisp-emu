namespace AISpace.Network.Packets.Area;

public sealed class RoboAiscriptEndRequest : IIncomingPacket<RoboAiscriptEndRequest>
{
    public uint RoboId { get; init; }

    public static RoboAiscriptEndRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new RoboAiscriptEndRequest { RoboId = reader.ReadUInt() };
    }
}
