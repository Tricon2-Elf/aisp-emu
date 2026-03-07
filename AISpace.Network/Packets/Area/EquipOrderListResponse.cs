using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class EquipOrderListResponse : IPacket<EquipOrderListResponse>
{
    public static EquipOrderListResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)0); //Result
        writer.Write((uint)0); // chara_order
        writer.Write((uint)0); // job_order
        return writer.ToBytes();
    }
}
