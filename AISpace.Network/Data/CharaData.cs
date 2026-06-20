namespace AISpace.Network.Data;

public class CharaData(uint slotId, uint modelId, string name)
{
    public CharaVisual Visual = new(BloodType.A, 1, 1, 1, 2, 0, 0);
    public MovementData MoveData = new(0, 0, 0, 0, 0);
    public RotationData Rotation = new(0, 0, 0, 0);
    public TpsBattleParams TpsBattle = new();
    public ActionInitParams ActionInit = new();
    public CharacterStateParams CharState = new();

    public uint CharacterRefId = 0;
    public float ActionVecX = 0; // Not used?
    public float ActionVecY = 0; // Not used?
    public uint Unk_164 = 0; // Not used?
    public uint ParamState = 0; // Not used?

    public List<ItemSlotInfo> Equips = new(30);

    public void AddEquip(uint id, uint socket)
    {
        Equips.Add(new ItemSlotInfo(id, socket));
    }

    public void AddEquip(IEnumerable<CharacterEquipSlot> equipment, Func<CharacterEquipSlot, uint> resolveSocket)
    {
        for (byte slot = 0; slot < 30; slot++)
        {
            var eq = equipment.FirstOrDefault(e => e.SlotIndex == slot);
            AddEquip(eq.ItemId, eq.ItemId != 0 ? resolveSocket(eq) : 0);
        }
    }

    public byte[] ToBytes()
    {
        while (Equips.Count < 30)
            AddEquip(0, 0);

        var writer = new PacketWriter();
        writer.Write(slotId);
        writer.Write(modelId);
        writer.WriteFixedString(name, 37, "SHIFT_JIS");
        writer.Write(Visual.ToBytes());
        writer.Write(CharacterRefId);
        writer.Write(Rotation.ToBytes());
        writer.Write(MoveData.ToBytes());
        writer.Write(ActionVecX);
        writer.Write(ActionVecY);
        for (int i = 0; i < 30; i++)
            writer.Write(Equips[i].ToBytes());
        writer.Write(Unk_164);
        writer.Write(ParamState);
        writer.Write(ActionInit.ToBytes());
        writer.Write(TpsBattle.ToBytes());
        writer.Write(CharState.ToBytes());
        return writer.ToBytes();
    }
}
