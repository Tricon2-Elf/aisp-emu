using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class ItemTryEquipResetRequest(uint objId) : IIncomingPacket<ItemTryEquipResetRequest>
{
    public uint ObjId = objId;

    public static ItemTryEquipResetRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var objId = reader.ReadUInt();
        return new ItemTryEquipResetRequest(objId);
    }
}
