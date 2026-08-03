namespace AISpace.Network.Packets.Area;

/// <summary>
/// recv_notify_robo_furnact_end (0xB45C). Broadcast when a Robo finishes using furniture.
/// Payload: UInt roboid.
/// </summary>
public sealed class NotifyRoboFurnactEnd(uint roboId) : IOutgoingPacket
{
    public uint RoboId { get; } = roboId;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(RoboId);
        return writer.ToBytes();
    }
}
