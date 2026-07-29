namespace AISpace.Network.Data;

public sealed class HitPointData
{
    public const int WireSize = 18;

    public uint Current { get; set; }
    public uint BaseMaximum { get; set; }
    public uint MaximumBonus { get; set; }
    public uint MaximumPenalty { get; set; }
    public byte CurrentHearts { get; set; }
    public byte MaximumHearts { get; set; }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Current);
        writer.Write(BaseMaximum);
        writer.Write(MaximumBonus);
        writer.Write(MaximumPenalty);
        writer.Write(CurrentHearts);
        writer.Write(MaximumHearts);
        return writer.ToBytes();
    }

    public static HitPointData FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new HitPointData
        {
            Current = reader.ReadUInt(),
            BaseMaximum = reader.ReadUInt(),
            MaximumBonus = reader.ReadUInt(),
            MaximumPenalty = reader.ReadUInt(),
            CurrentHearts = reader.ReadByte(),
            MaximumHearts = reader.ReadByte(),
        };
    }
}

public sealed class StaminaData
{
    public const int WireSize = 16;

    public float Current { get; set; }
    public float RecoveryRate { get; set; }
    public uint CostReductionBonus { get; set; }
    public uint CostReductionPenalty { get; set; }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Current);
        writer.Write(RecoveryRate);
        writer.Write(CostReductionBonus);
        writer.Write(CostReductionPenalty);
        return writer.ToBytes();
    }

    public static StaminaData FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new StaminaData
        {
            Current = reader.ReadFloat(),
            RecoveryRate = reader.ReadFloat(),
            CostReductionBonus = reader.ReadUInt(),
            CostReductionPenalty = reader.ReadUInt(),
        };
    }
}

public sealed class TankData
{
    public const int WireSize = 16;

    public uint Current { get; set; }
    public uint BaseMaximum { get; set; }
    public uint MaximumBonus { get; set; }
    public uint MaximumPenalty { get; set; }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Current);
        writer.Write(BaseMaximum);
        writer.Write(MaximumBonus);
        writer.Write(MaximumPenalty);
        return writer.ToBytes();
    }

    public static TankData FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new TankData
        {
            Current = reader.ReadUInt(),
            BaseMaximum = reader.ReadUInt(),
            MaximumBonus = reader.ReadUInt(),
            MaximumPenalty = reader.ReadUInt(),
        };
    }
}

/// <summary>
/// Five TPS battle-ability values. The update packets address these by an
/// ability index in the range 0-4.
/// </summary>
public sealed class BattleAbilityValues
{
    public const int Count = 5;
    public const int WireSize = Count * sizeof(uint);

    public uint[] Values { get; set; } = new uint[Count];

    public byte[] ToBytes()
    {
        if (Values.Length != Count)
            throw new InvalidOperationException(
                $"BattleAbilityValues must contain exactly {Count} values."
            );

        var writer = new PacketWriter();
        foreach (var value in Values)
            writer.Write(value);
        return writer.ToBytes();
    }

    public static BattleAbilityValues FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var values = new uint[Count];
        for (var i = 0; i < values.Length; i++)
            values[i] = reader.ReadUInt();
        return new BattleAbilityValues { Values = values };
    }
}

public sealed class CosplayProgressData
{
    public const int WireSize = sizeof(uint) + LevelProgressData.WireSize;

    public uint CosplayId { get; set; }
    public LevelProgressData Progress { get; set; } = new();

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(CosplayId);
        writer.Write(Progress.ToBytes());
        return writer.ToBytes();
    }

    public static CosplayProgressData FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new CosplayProgressData
        {
            CosplayId = reader.ReadUInt(),
            Progress = LevelProgressData.FromBytes(reader.ReadBytes(LevelProgressData.WireSize)),
        };
    }
}

/// <summary>
/// Complete 175-byte TPS combat state read by the client's
/// <c>sub_798D80</c>.
/// </summary>
public sealed class TpsBattleData
{
    public const int WireSize = 175;

    public HitPointData HitPoints { get; set; } = new();
    public StaminaData Stamina { get; set; } = new();
    public TankData Tank { get; set; } = new();
    public BattleAbilityValues BaseAbilities { get; set; } = new();
    public BattleAbilityValues AbilityModifierType0 { get; set; } = new();
    public BattleAbilityValues AbilityModifierType1 { get; set; } = new();
    public BattleAbilityValues AbilityModifierType2 { get; set; } = new();
    public ulong StatusEffectFlags { get; set; }
    public uint ActionFlags { get; set; }
    public uint ActiveSkillId { get; set; }
    public CosplayProgressData Cosplay { get; set; } = new();

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(HitPoints.ToBytes());
        writer.Write(Stamina.ToBytes());
        writer.Write(Tank.ToBytes());
        writer.Write(BaseAbilities.ToBytes());
        writer.Write(AbilityModifierType0.ToBytes());
        writer.Write(AbilityModifierType1.ToBytes());
        writer.Write(AbilityModifierType2.ToBytes());
        writer.Write(StatusEffectFlags);
        writer.Write(ActionFlags);
        writer.Write(ActiveSkillId);
        writer.Write(Cosplay.ToBytes());
        return writer.ToBytes();
    }

    public static TpsBattleData FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length < WireSize)
            throw new ArgumentException(
                $"TpsBattleData requires at least {WireSize} bytes.",
                nameof(data)
            );

        var reader = new PacketReader(data);
        return new TpsBattleData
        {
            HitPoints = HitPointData.FromBytes(reader.ReadBytes(HitPointData.WireSize)),
            Stamina = StaminaData.FromBytes(reader.ReadBytes(StaminaData.WireSize)),
            Tank = TankData.FromBytes(reader.ReadBytes(TankData.WireSize)),
            BaseAbilities = BattleAbilityValues.FromBytes(
                reader.ReadBytes(BattleAbilityValues.WireSize)
            ),
            AbilityModifierType0 = BattleAbilityValues.FromBytes(
                reader.ReadBytes(BattleAbilityValues.WireSize)
            ),
            AbilityModifierType1 = BattleAbilityValues.FromBytes(
                reader.ReadBytes(BattleAbilityValues.WireSize)
            ),
            AbilityModifierType2 = BattleAbilityValues.FromBytes(
                reader.ReadBytes(BattleAbilityValues.WireSize)
            ),
            StatusEffectFlags = reader.ReadULong(),
            ActionFlags = reader.ReadUInt(),
            ActiveSkillId = reader.ReadUInt(),
            Cosplay = CosplayProgressData.FromBytes(reader.ReadBytes(CosplayProgressData.WireSize)),
        };
    }
}
