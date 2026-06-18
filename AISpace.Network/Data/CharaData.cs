namespace AISpace.Network.Data;

public class CharaData(uint slotId, uint modelId, string name)
{
    public CharaVisual Visual = new(BloodType.A, 1, 1, 1, 2, 0, 0);
    public MovementData moveData = new(0, 0, 0, 0, 0);

    //X-4069.790 Y-0.043 Z-2813.927
    public List<ItemSlotInfo> Equips = new(30);

    public void AddEquip(uint id, uint socket)
    {
        Equips.Add(new ItemSlotInfo(id, socket));
    }

    public void AddEquip(IEnumerable<CharacterEquipSlot> equipment, Func<CharacterEquipSlot, uint> resolveSocket)
    {
        foreach (var eq in equipment)
            AddEquip(eq.ItemId, resolveSocket(eq));
    }

    public byte[] ToBytes()
    {
        while (Equips.Count < 30)
            AddEquip(0, 0);

        var writer = new PacketWriter();
        writer.Write(slotId); // m_SlotId (4)
        writer.Write(modelId); // m_Model (4)
        writer.WriteFixedString(name, 37, "SHIFT_JIS");
        writer.Write(Visual.ToBytes()); // ReadAvatarVisual (19)
        writer.Write(0u); // m_pCharacter (4)
        writer.Write(0f); //Quaternion X
        writer.Write(0f); //Quaternion Y
        writer.Write(0f); //Quaternion Z
        writer.Write(0f); //Quaternion W
        writer.Write(moveData.ToBytes()); // ReadMoveData (14)
        writer.Write(0f); // Vec2 float_6c (8)
        writer.Write(0f);
        for (int i = 0; i < 30; i++) // m_Equipment 30×(id,socket) - no count
            writer.Write(Equips[i].ToBytes());
        writer.Write(0u); // dword_164 (4)
        writer.Write(0u); // dword_168 (4)
        writer.Write(0u); // dword_16c (4)
        writer.Write(0f); // Vec2 float_170 (8)
        writer.Write(0f);
        // field_178 (sub_798D80): 0 bytes padding so total CharaData = 383
        writer.Write((byte)0); // field_240: byte_0 (1)
        writer.Write(0L); // field_240: dword_8 (8)
        writer.Write(0L); // field_240: dword_10 (8)
        return writer.ToBytes(); // 383 bytes
    }
}
