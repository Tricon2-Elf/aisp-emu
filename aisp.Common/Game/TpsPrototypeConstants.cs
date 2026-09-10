namespace aisp.Common.Game;

/// <summary>Hardcoded values for the TPS combat prototype ported from aisp-emu2.</summary>
public static class TpsPrototypeConstants
{
    public const uint MobObjectId = 2000001;
    public const uint MobModelId = 7001010;
    public const uint WaterGunItemId = 12300010;

    /// <summary>Paper-doll handheld cell (prefix 123 → CharacterEquipmentSlotIndex.Handheld).</summary>
    public const byte WaterGunEquipSlot = 19;

    /// <summary>Client attach bit 1&lt;&lt;19 for the handheld/right-hand cell.</summary>
    public const uint WaterGunSocketBit = 1u << 19;

    /// <summary>Seeded TPS(UDX) combat test map.</summary>
    public const uint TpsUdxMapId = 40_990_200;

    public const uint MapIdMinInclusive = 40_000_000;
    public const uint MapIdMaxExclusive = 50_000_000;

    public const float PlayerSpawnX = -9200f;
    public const float PlayerSpawnY = 0.1f;
    public const float PlayerSpawnZ = -14485f;

    public const float MobSpawnX = PlayerSpawnX;
    public const float MobSpawnY = 0.1f;
    public const float MobSpawnZ = PlayerSpawnZ + 200f;
    public const int MobSpawnRotation = 180;

    public const uint MissionTimeLimitSeconds = 300;
    public const uint MissionTargetCount = 40;

    public const int DefaultHitPoints = 100;
    public const uint DefaultTank = 100;
    public const int AttackDamage = 25;
    public const uint TankConsumePerShot = 1;

    /// <summary>
    /// CTPSActionReport switch value that queues the fire motion and the local
    /// <c>send_battle_attack_exec</c> callback (decomp <c>sub_4E63C0</c> case 4).
    /// That path sets TPS phase 5, which blocks another <c>send_battle_attack_start</c>.
    /// </summary>
    public const uint BattleReportAttackAction = 4;

    /// <summary>
    /// CTPSActionReport switch value that queues <c>CTPSActStateAction</c> phase 0
    /// (decomp <c>sub_4E63C0</c> case 8). Required after exec so the player can fire again.
    /// </summary>
    public const uint BattleReportRecoverAction = 8;

    public static readonly uint[] DefaultSkills = [200090, 200000, 200020];
}
