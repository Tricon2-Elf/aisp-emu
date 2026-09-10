namespace aisp.Common.Game;

/// <summary>Hardcoded values for the TPS combat prototype ported from aisp-emu2.</summary>
public static class TpsPrototypeConstants
{
    public const uint MobObjectId = 2000001;
    public const uint MobModelId = 7001010;
    public const uint WaterGunItemId = 12300010;
    public const byte WaterGunEquipSlot = 12;

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

    public static readonly uint[] DefaultSkills = [200090, 200000, 200020];
}
