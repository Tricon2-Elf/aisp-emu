using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public class NotifyBattleRaiseStart(
    uint objId,
    IEnumerable<ItemEquipEntry>? equips = null,
    byte isRestart = 0
) : IOutgoingPacket
{
    private readonly List<ItemEquipEntry> _equips =
        equips?.ToList() ?? [new ItemEquipEntry(12300010, 1u << 19)];

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(objId);
        writer.Write((uint)_equips.Count);
        foreach (var eq in _equips)
        {
            writer.Write(eq.ItemId);
            writer.Write(eq.SocketBit);
        }
        writer.Write(isRestart);
        return writer.ToBytes();
    }
}
