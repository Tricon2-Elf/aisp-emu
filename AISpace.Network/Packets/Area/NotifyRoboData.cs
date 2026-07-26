using AISpace.Network.Data;

namespace AISpace.Network.Packets.Area;

public sealed class NotifyRoboData(uint result, RoboData roboData) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(roboData.ToBytes());
        return writer.ToBytes();
    }
}
