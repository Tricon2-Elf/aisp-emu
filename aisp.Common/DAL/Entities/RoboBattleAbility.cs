namespace aisp.Common.DAL.Entities;

public enum RoboBattleAbilitySet : byte
{
    Base = 0,
    ModifierType0 = 1,
    ModifierType1 = 2,
    ModifierType2 = 3,
}

public sealed class RoboBattleAbility
{
    public int CharacterId { get; set; }
    public uint RoboId { get; set; }
    public RoboBattleAbilitySet AbilitySet { get; set; }
    public byte AbilityIndex { get; set; }
    public uint Value { get; set; }
    public RoboTpsBattleData TpsBattleData { get; set; } = default!;
}
