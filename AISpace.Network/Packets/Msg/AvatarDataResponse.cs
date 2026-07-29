using AISpace.Network.Data;

namespace AISpace.Network.Packets.Msg;

public class AvatarDataResponse(
    uint avatarId,
    string name,
    uint modelId,
    uint islandId,
    uint slotId
) : IOutgoingPacket
{
    public CharaVisual Visual = new(BloodType.A, 1, 1, 1, 2, 0, 0);
    public List<ItemSlotInfo> Equips = new(30);

    //equips?

    public void AddEquip(uint id, uint socket)
    {
        Equips.Add(new ItemSlotInfo(id, socket));
    }

    public void AddEquip(
        IEnumerable<CharacterEquipSlot> equipment,
        Func<CharacterEquipSlot, uint> resolveSocket
    )
    {
        for (byte slot = 0; slot < 30; slot++)
        {
            if (!equipment.Any(e => e.SlotIndex == slot))
            {
                AddEquip(0, 0);
                continue;
            }

            var eq = equipment.First(e => e.SlotIndex == slot);
            AddEquip(eq.ItemId, resolveSocket(eq));
        }
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(avatarId); // AvatarId
        writer.Write(name, "utf-8");
        writer.Write(modelId);
        writer.Write(Visual.ToBytes());
        writer.Write(islandId);
        writer.Write(slotId);
        foreach (var equip in Equips)
            writer.Write(equip.ToBytes());
        return writer.ToBytes();
    }
}
