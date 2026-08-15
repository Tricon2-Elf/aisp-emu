using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public class ItemTryEquipReplacedNotify(uint objId, IEnumerable<ItemEquipEntry> equips)
    : IOutgoingPacket
{
    public uint ObjId { get; set; } = objId;
    public IReadOnlyList<ItemEquipEntry> Equips { get; set; } = equips.ToList();

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ObjId);
        writer.Write((uint)Equips.Count);
        foreach (var equip in Equips)
            writer.Write(equip.ToBytes());
        return writer.ToBytes();
    }
}
