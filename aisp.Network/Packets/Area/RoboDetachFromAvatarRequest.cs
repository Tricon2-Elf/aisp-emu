namespace aisp.Network.Packets.Area;

/// <summary>
/// Ends the avatar's current conversation interaction with a Robo.
/// Payload: UInt RoboId.
/// </summary>
public sealed class RoboDetachFromAvatarRequest : IIncomingPacket<RoboDetachFromAvatarRequest>
{
    public uint RoboId { get; init; }

    public static RoboDetachFromAvatarRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new RoboDetachFromAvatarRequest { RoboId = reader.ReadUInt() };
    }
}
