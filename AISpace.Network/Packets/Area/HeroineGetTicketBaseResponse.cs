using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class HeroineGetTicketBaseResponse : IPacket<HeroineGetTicketBaseResponse>
{
    public static HeroineGetTicketBaseResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)0); // heroine_tickets
        return writer.ToBytes();
    }
}
