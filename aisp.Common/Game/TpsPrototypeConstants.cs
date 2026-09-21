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

    /// <summary>
    /// <c>CCharaEquip::CreateItemEquipment</c> selects <c>CHandAttachEquipment</c> (weapon grip)
    /// when catalog Socket1 has this bit. Without it the client takes <c>CHandAttachEquipment2</c>
    /// (bag/prop), which can play the fire motion with no visible handheld mesh.
    /// </summary>
    public const uint WaterGunWeaponAttachBit = 0x20000000;

    /// <summary>Catalog Socket1 sent for 12300010 so the live mesh uses the weapon-hand class.</summary>
    public const uint WaterGunCatalogSocket = WaterGunSocketBit | WaterGunWeaponAttachBit;

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

    /// <summary>XZ radius of the prototype mob hit cylinder (matches <c>CharaData.CollisionRadius</c> on spawn).</summary>
    public const float MobCollisionRadius = 60f;

    /// <summary>Y extent of the prototype mob hit cylinder from spawn Y (matches <c>CharaData.TpsActionVerticalRange</c>).</summary>
    public const float MobTpsActionVerticalRange = 60f;

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
    /// CTPSActionReport shot commit (decomp <c>sub_4E63C0</c> case 5): muzzle FX
    /// (<c>CTPSFxCharaCreate</c> / <c>CTPSTimeBlaze</c>) and gunshot SE
    /// (<c>CTPSSEPlay</c> / <c>CTPSVoicePlay</c>) from the skill table.
    /// Action 4 only queues the fire motion and the local exec callback.
    /// </summary>
    public const uint BattleReportShotAction = 5;

    /// <summary>
    /// How long to leave action 5's <c>CTPSTimeBlaze</c> running before action 8.
    /// Matches the decompile default when the skill row is missing (<c>v88 = 1.0</c>).
    /// Sending recover in the same tick as the shot report cuts the FX/SE off.
    /// </summary>
    public const int ShotRecoverDelayMs = 1000;

    /// <summary>
    /// CTPSActionReport switch value that queues <c>CTPSActStateAction</c> phase 0
    /// (decomp <c>sub_4E63C0</c> case 8). Required after attack exec (phase 5)
    /// so the player can fire again. Does not clear the dash run flag.
    /// </summary>
    public const uint BattleReportRecoverAction = 8;

    /// <summary>
    /// CTPSActionReport dash-begin (decomp <c>sub_4E63C0</c> case 0x1C): TPS phase 8
    /// plus the local dash motion callback.
    /// </summary>
    public const uint BattleReportDashAction = 0x1C;

    /// <summary>
    /// CTPSActionReport dash-end (decomp <c>sub_4E63C0</c> case 0x1D). Skill id must
    /// be 0, 1, or 2 — any other value is a no-op. Skill 0 queues phase 12, the
    /// dash-end motion, <c>sub_4E06E0</c> (clears controller dash-run flag +224),
    /// then phase 0.
    /// </summary>
    public const uint BattleReportDashEndAction = 0x1D;

    /// <summary>Skill field that selects the skill-0 branch of action 0x1D.</summary>
    public const uint BattleReportDashEndSkillId = 0;

    public static readonly uint[] DefaultSkills = [200090, 200000, 200020];
}
