using aisp.Network;

namespace aisp.Network.Packets.Area;

public class ItemEquipEndRequest : IIncomingPacket<ItemEquipEndRequest>
{
    public uint ObjId { get; set; }

    public ItemEquipEndRequest(uint objId)
    {
        ObjId = objId;
    }

    public static ItemEquipEndRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var objId = reader.ReadUInt();
        return new ItemEquipEndRequest(objId);
    }
}
