using aisp.Network;

namespace aisp.Network.Packets.Area;

public class NotifyUpdateTank(uint objId, uint tank) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(objId);
        writer.Write(tank);
        return writer.ToBytes();
    }
}
