using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class EquipOrderListResponse : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)0); //Result
        writer.Write((uint)0); // chara_order
        writer.Write((uint)0); // job_order
        return writer.ToBytes();
    }
}
