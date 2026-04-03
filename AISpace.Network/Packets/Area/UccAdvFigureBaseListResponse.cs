using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class UccAdvFigureBaseListResponse : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)0); //Result
        writer.Write((uint)0); // adv_figures
        return writer.ToBytes();
    }
}
