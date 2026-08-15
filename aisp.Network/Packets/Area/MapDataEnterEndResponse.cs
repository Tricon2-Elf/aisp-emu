using aisp.Network;

namespace aisp.Network.Packets.Area;

public class MapDataEnterEndResponse : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)0); //Result
        return writer.ToBytes();
    }
}
