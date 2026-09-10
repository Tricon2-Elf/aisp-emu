using aisp.Network;

namespace aisp.Network.Packets.Area;

public class NotifyMissionPartyStartOkUpdate(uint objId, uint reason = 0) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(objId);
        writer.Write(reason);
        return writer.ToBytes();
    }
}
