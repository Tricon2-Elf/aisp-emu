using AISpace.Network;

namespace AISpace.Network.Packets.Area;

/// <param name="yawDegrees">Furniture yaw in degrees (converted to wire half-degrees).</param>
/// <param name="pitchDegrees">Furniture pitch in degrees (converted to wire half-degrees).</param>
public class MyRoomNotifyFurniture(uint ownerId, uint serialId, uint actionType, uint itemId, float x, float y, float z, int yawDegrees = 0, int pitchDegrees = 0, bool active = true) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ownerId);
        writer.Write(serialId);
        writer.Write(actionType);
        writer.Write(itemId);
        writer.Write(x);
        writer.Write(y);
        writer.Write(z);
        writer.Write(YawEncoding.ToWireByte(yawDegrees));
        writer.Write(YawEncoding.ToWireByte(pitchDegrees));
        writer.Write(active ? 1u : 0u);
        return writer.ToBytes();
    }
}
