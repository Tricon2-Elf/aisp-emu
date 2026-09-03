namespace aisp.Network.Data;

public class CharaData(uint slotId, uint modelId, string name)
{
    public const int WireSize = 566;
    public const int EquipmentSlotCount = 30;

    public uint SlotId { get; set; } = slotId;
    public uint ModelId { get; set; } = modelId;
    public string Name { get; set; } = name;
    public CharaVisual Visual = new(BloodType.A, 1, 1, 1, 2, 0, 0);
    public uint CharacterParameterId { get; set; }
    public CharacterMapData Map { get; set; } = new();
    public float TpsActionReferenceX { get; set; }
    public float TpsActionReferenceY { get; set; }
    public uint ClientReserved { get; set; }

    /// <summary>Name plate variant (client CChara::SetNamePlate), a role the client's layout names: 0 none, 1 celebrity (pink), 2 GM (dark purple), 3 penalised user (red), 4 ordinary NPC (purple, the original's NPC plate), 5 official NPC (blue), 6 event user (orange with a yellow border), 0xFFFFFFFF creator/staff NPC (green); anything else draws no plate. Not a job id.</summary>
    public uint NamePlate { get; set; }
    public uint TpsActionProfileId { get; set; }
    public float CollisionRadius { get; set; }
    public float TpsActionVerticalRange { get; set; }
    public TpsBattleData Battle { get; set; } = new();
    public LevelProgressData Progress { get; set; } = new();

    public MovementData Movement
    {
        get => Map.Movement;
        set => Map.Movement = value;
    }

    public List<ItemSlotInfo> Equips = new(EquipmentSlotCount);

    public void AddEquip(uint id, uint socket)
    {
        Equips.Add(new ItemSlotInfo(id, socket));
    }

    public void AddEquip(
        IEnumerable<CharacterEquipSlot> equipment,
        Func<CharacterEquipSlot, uint> resolveSocket
    )
    {
        for (byte slot = 0; slot < EquipmentSlotCount; slot++)
        {
            var eq = equipment.FirstOrDefault(e => e.SlotIndex == slot);
            AddEquip(eq.ItemId, eq.ItemId != 0 ? resolveSocket(eq) : 0);
        }
    }

    public byte[] ToBytes()
    {
        while (Equips.Count < EquipmentSlotCount)
            AddEquip(0, 0);

        var writer = new PacketWriter();
        writer.Write(SlotId);
        writer.Write(ModelId);
        writer.WriteFixedString(Name, 37);
        writer.Write(Visual.ToBytes());
        writer.Write(CharacterParameterId);
        writer.Write(Map.ToBytes());
        writer.Write(TpsActionReferenceX);
        writer.Write(TpsActionReferenceY);
        for (var i = 0; i < EquipmentSlotCount; i++)
            writer.Write(Equips[i].ToBytes());
        writer.Write(ClientReserved);
        writer.Write(NamePlate);
        writer.Write(TpsActionProfileId);
        writer.Write(CollisionRadius);
        writer.Write(TpsActionVerticalRange);
        writer.Write(Battle.ToBytes());
        writer.Write(Progress.ToBytes());
        return writer.ToBytes();
    }

    public static CharaData FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length < WireSize)
            throw new ArgumentException(
                $"CharaData requires at least {WireSize} bytes.",
                nameof(data)
            );

        var reader = new PacketReader(data);
        var result = new CharaData(reader.ReadUInt(), reader.ReadUInt(), reader.ReadFixedString(37))
        {
            Visual = CharaVisual.FromBytes(reader.ReadBytes(19)),
            CharacterParameterId = reader.ReadUInt(),
            Map = CharacterMapData.FromBytes(reader.ReadBytes(CharacterMapData.WireSize)),
            TpsActionReferenceX = reader.ReadFloat(),
            TpsActionReferenceY = reader.ReadFloat(),
        };

        for (var i = 0; i < EquipmentSlotCount; i++)
            result.Equips.Add(new ItemSlotInfo(reader.ReadUInt(), reader.ReadUInt()));

        result.ClientReserved = reader.ReadUInt();
        result.NamePlate = reader.ReadUInt();
        result.TpsActionProfileId = reader.ReadUInt();
        result.CollisionRadius = reader.ReadFloat();
        result.TpsActionVerticalRange = reader.ReadFloat();
        result.Battle = TpsBattleData.FromBytes(reader.ReadBytes(TpsBattleData.WireSize));
        result.Progress = LevelProgressData.FromBytes(reader.ReadBytes(LevelProgressData.WireSize));
        return result;
    }
}
