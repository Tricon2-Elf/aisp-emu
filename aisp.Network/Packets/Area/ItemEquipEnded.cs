using aisp.Network;

namespace aisp.Network.Packets.Area;

public class ItemEquipEnded : IOutgoingPacket
{
    public uint ObjId { get; set; }

    public ItemEquipEnded(uint objId)
    {
        ObjId = objId;
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ObjId);
        return writer.ToBytes();
    }
}
