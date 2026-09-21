using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_notify_update_heart: objid + remaining hearts (decomp logs <c>objid</c>, <c>heart</c>).
/// </summary>
public class NotifyUpdateHeart(uint objId, uint hearts) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(objId);
        writer.Write(hearts);
        return writer.ToBytes();
    }
}
