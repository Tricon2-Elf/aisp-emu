using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class MyRoomNotifyFurniture(uint ownerId, uint serialId, uint actionType, uint itemId, float x, float y, float z, byte yawHalfDegrees = 0, byte pitchHalfDegrees = 0, bool active = true) : IOutgoingPacket
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
        writer.Write(yawHalfDegrees);
        writer.Write(pitchHalfDegrees);
        writer.Write(active ? 1u : 0u);
        return writer.ToBytes();
    }
}
