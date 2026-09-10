using aisp.Network;

namespace aisp.Network.Packets.Area;

public class NotifyUpdateHitpoint(uint objId, uint hitpoint) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(objId); // Who we are hitting
        writer.Write(hitpoint); // New HP
        return writer.ToBytes();
    }
}
