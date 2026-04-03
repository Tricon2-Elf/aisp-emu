using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class NiconiCommonsBaseListResponse : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)0); //Result
        writer.Write((uint)0); // commons_base
        return writer.ToBytes();
    }
}
