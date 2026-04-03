using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class HeroineGetTicketBaseResponse : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)0); // heroine_tickets
        return writer.ToBytes();
    }
}
