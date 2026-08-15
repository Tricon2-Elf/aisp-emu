using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_notify_robo_furnact_start (0xB77E). Broadcast when a Robo starts using furniture.
/// Payload: UInt roboid, UInt furnid, MovementData start (14 bytes).
/// </summary>
public sealed class NotifyRoboFurnactStart(uint roboId, uint furnitureId, MovementData start)
    : IOutgoingPacket
{
    public uint RoboId { get; } = roboId;
    public uint FurnitureId { get; } = furnitureId;
    public MovementData Start { get; } = start;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(RoboId);
        writer.Write(FurnitureId);
        writer.Write(Start.ToBytes());
        return writer.ToBytes();
    }
}
