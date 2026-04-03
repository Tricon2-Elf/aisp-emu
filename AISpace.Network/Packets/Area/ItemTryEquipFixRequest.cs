using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class ItemTryEquipFixRequest(uint objId) : IIncomingPacket<ItemTryEquipFixRequest>
{
    public uint ObjId = objId;

    public static ItemTryEquipFixRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var objId = reader.ReadUInt();
        return new ItemTryEquipFixRequest(objId);
    }
}
