namespace aisp.Network.Packets.Area;

/// <summary>
/// Starts the client protocol's "attach" handshake used to talk to an owned Robo.
/// Payload: UInt RoboId.
/// </summary>
public sealed class RoboAttachRequest : IIncomingPacket<RoboAttachRequest>
{
    public uint RoboId { get; init; }

    public static RoboAttachRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new RoboAttachRequest { RoboId = reader.ReadUInt() };
    }
}
