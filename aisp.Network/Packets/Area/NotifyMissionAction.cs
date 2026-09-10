using aisp.Network;

namespace aisp.Network.Packets.Area;

public class NotifyMissionAction(uint actionType, uint param1 = 0) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(actionType);
        writer.Write(param1);
        return writer.ToBytes();
    }
}
