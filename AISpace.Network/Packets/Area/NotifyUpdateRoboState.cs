using AISpace.Network.Data;

namespace AISpace.Network.Packets.Area;

public sealed class NotifyUpdateRoboState(uint roboId, uint objectId, uint state, MovementData? move = null) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(roboId);
        writer.Write(objectId);
        writer.Write(state);
        // sub_798720: quaternion (4×float) + MoveData (14)
        writer.Write(0f);
        writer.Write(0f);
        writer.Write(0f);
        writer.Write(1f);
        writer.Write((move ?? new MovementData(0, 0, 0, 0, MovementType.Stopped)).ToBytes());
        return writer.ToBytes();
    }
}
