namespace AISpace.Common.Game;

public class CharaData(uint slotId, uint modelId, string name)
{
    public CharaVisual Visual = new(BloodType.A, 1, 1, 1, 2, 0, 0);
    public MovementData moveData = new(0, 0, 0, 0, 0);
    public List<Game.ItemSlotInfo> Equips = new(30);

    public void AddEquip(uint id, uint socket)
    {
        if (Equips.Count < 30)
            Equips.Add(new ItemSlotInfo(id, socket));
    }

    public byte[] ToBytes()
    {
        var writer = new Network.PacketWriter();
        writer.Write(slotId); // m_SlotId (4)
        writer.Write(modelId); // m_Model (4)
        writer.WriteFixedString(name, 37, "SHIFT_JIS"); // m_Name (37)
        writer.Write(Visual.ToBytes()); // m_Visual (19)
        
        writer.Write(0u); // m_pCharacter (4) - Указатель, должен быть 0
        
        // Вращение (Quaternion: X, Y, Z, W) - 16 байт
        writer.Write(0f); 
        writer.Write(0f); 
        writer.Write(0f); 
        writer.Write(1f); 

        writer.Write(moveData.ToBytes()); // Position (14)
        
        writer.Write(0f); // float_6c (Vec2: 8 байт)
        writer.Write(0f);

        // Список предметов: 30 слотов по 8 байт (ID + Socket) = 240 байт
        for (int i = 0; i < 30; i++)
        {
            if (i < Equips.Count)
                writer.Write(Equips[i].ToBytes());
            else
            {
                writer.Write(0u); // ItemID
                writer.Write((uint)i); // Socket
            }
        }

        writer.Write(0u); // dword_164 (4)
        writer.Write(0u); // dword_168 (4)
        writer.Write(0u); // dword_16c (4)
        writer.Write(0f); // Vec2 float_170 (8)
        writer.Write(0f);
        
        writer.Write((byte)0); // field_240 (1)
        writer.Write(0L);      // dword_8 (8)
        writer.Write(0L);      // dword_10 (8)
        
        return writer.ToBytes(); // Ровно 383 байта
    }
}