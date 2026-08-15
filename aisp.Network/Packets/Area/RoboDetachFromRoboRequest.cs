namespace aisp.Network.Packets.Area;

/// <summary>
/// Reports that the Robo-side controller could not establish or retain an avatar interaction.
/// Payload: UInt RoboId.
/// </summary>
public sealed class RoboDetachFromRoboRequest : IIncomingPacket<RoboDetachFromRoboRequest>
{
    public uint RoboId { get; init; }

    public static RoboDetachFromRoboRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new RoboDetachFromRoboRequest { RoboId = reader.ReadUInt() };
    }
}
