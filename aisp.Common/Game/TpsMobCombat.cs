using System.Numerics;

namespace aisp.Common.Game;

/// <summary>
/// Prototype mob return-fire helpers. The mission loop runs the attack sequence.
/// </summary>
public static class TpsMobCombat
{
    public static bool IsPlayerInAttackRange(Vector3 mob, Vector3 player)
    {
        var dx = player.X - mob.X;
        var dz = player.Z - mob.Z;
        var range = TpsPrototypeConstants.MobAttackRange;
        return dx * dx + dz * dz <= range * range;
    }

    public static int YawToward(float dx, float dz)
    {
        if (dx * dx + dz * dz < 1e-6f)
            return TpsPrototypeConstants.MobSpawnRotation;

        var deg = (int)MathF.Round(MathF.Atan2(dx, dz) * (180f / MathF.PI));
        return ((deg % 360) + 360) % 360;
    }
}
