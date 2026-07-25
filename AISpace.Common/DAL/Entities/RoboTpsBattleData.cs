namespace AISpace.Common.DAL.Entities;

public sealed class RoboTpsBattleData
{
    public int CharacterId { get; set; }
    public uint RoboId { get; set; }
    public Robo Robo { get; set; } = default!;

    public float ActionReferenceX { get; set; }
    public float ActionReferenceY { get; set; }
    public uint ActionProfileId { get; set; }
    public float CollisionRadius { get; set; }
    public float ActionVerticalRange { get; set; }

    public uint HitPointsCurrent { get; set; }
    public uint HitPointsBaseMaximum { get; set; }
    public uint HitPointsMaximumBonus { get; set; }
    public uint HitPointsMaximumPenalty { get; set; }
    public byte CurrentHearts { get; set; }
    public byte MaximumHearts { get; set; }

    public float StaminaCurrent { get; set; }
    public float StaminaRecoveryRate { get; set; }
    public uint StaminaCostReductionBonus { get; set; }
    public uint StaminaCostReductionPenalty { get; set; }

    public uint TankCurrent { get; set; }
    public uint TankBaseMaximum { get; set; }
    public uint TankMaximumBonus { get; set; }
    public uint TankMaximumPenalty { get; set; }

    public ulong StatusEffectFlags { get; set; }
    public uint ActionFlags { get; set; }
    public uint ActiveSkillId { get; set; }

    public uint CosplayId { get; set; }
    public byte CosplayLevel { get; set; }
    public ulong CosplayStatusPoints { get; set; }
    public ulong CosplayExperience { get; set; }
    public ulong CosplayExperienceToNextLevel { get; set; }

    public ICollection<RoboBattleAbility> BattleAbilities { get; set; } = [];
}
