using aisp.Network;

namespace aisp.Network.Packets.Area;

public class ItemEquipStartRequest(uint objId) : IIncomingPacket<ItemEquipStartRequest>
{
    public uint ObjId = objId;

    public static ItemEquipStartRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new ItemEquipStartRequest(reader.ReadUInt());
    }
}
