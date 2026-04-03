using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class FurnitureGetBaseListResponse : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)0);
        writer.Write((uint)0);
        return writer.ToBytes();
    }
}
