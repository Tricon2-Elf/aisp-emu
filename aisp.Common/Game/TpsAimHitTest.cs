using System.Numerics;

namespace aisp.Common.Game;

/// <summary>
/// Free-aim hit test against the prototype mob's Y-up collision cylinder.
/// The client's <c>target_pos</c> is already a world raycast (walls clip the segment);
/// this only checks whether that aim lands on the mob volume.
/// </summary>
public static class TpsAimHitTest
{
    public static bool HitsPrototypeMob(Vector3 origin, Vector3 target)
    {
        var current = TpsCombatTestState.GetMobPosition();
        if (HitsPrototypeMob(origin, target, current))
            return true;

        // The client mesh may still be on spawn, mid-step, or already at the
        // wander destination. Accept all three so shots at the visible body hit.
        TpsCombatTestState.TryGetMobPath(
            TpsPrototypeConstants.MobObjectId,
            out var from,
            out var to
        );
        var spawn = new Vector3(
            TpsPrototypeConstants.MobSpawnX,
            TpsPrototypeConstants.MobSpawnY,
            TpsPrototypeConstants.MobSpawnZ
        );
        return HitsPrototypeMob(origin, target, from)
            || HitsPrototypeMob(origin, target, to)
            || HitsPrototypeMob(origin, target, spawn);
    }

    public static bool HitsPrototypeMob(Vector3 origin, Vector3 target, Vector3 mob)
    {
        var ymin = mob.Y - TpsPrototypeConstants.MobHitYPad;
        var height = TpsPrototypeConstants.MobHitHeight + TpsPrototypeConstants.MobHitYPad;
        var radius = TpsPrototypeConstants.MobHitRadius;

        // TPS camera often plants target_pos on the torso or the ground at the
        // mob's feet rather than along a feet-to-aim segment that clips the slab.
        return PointInYCylinder(target, mob, radius, ymin, ymin + height)
            || SegmentHitsYCylinder(origin, target, mob with { Y = ymin }, radius, height);
    }

    public static bool SegmentHitsPrototypeMob(Vector3 origin, Vector3 target) =>
        HitsPrototypeMob(origin, target);

    public static bool PointInYCylinder(
        Vector3 point,
        Vector3 cylinderBase,
        float radius,
        float ymin,
        float ymax
    )
    {
        if (point.Y < ymin || point.Y > ymax)
            return false;

        var dx = point.X - cylinderBase.X;
        var dz = point.Z - cylinderBase.Z;
        return dx * dx + dz * dz <= radius * radius;
    }

    /// <summary>
    /// Finite Y-up cylinder: XZ disc of <paramref name="radius"/> at <paramref name="cylinderBase"/>,
    /// extruded through <c>[base.Y, base.Y + height]</c>.
    /// </summary>
    public static bool SegmentHitsYCylinder(
        Vector3 origin,
        Vector3 target,
        Vector3 cylinderBase,
        float radius,
        float height
    )
    {
        if (radius <= 0f || height <= 0f)
            return false;

        var ymin = cylinderBase.Y;
        var ymax = cylinderBase.Y + height;
        var d = target - origin;
        var ox = origin.X - cylinderBase.X;
        var oz = origin.Z - cylinderBase.Z;
        var a = d.X * d.X + d.Z * d.Z;
        var b = 2f * (ox * d.X + oz * d.Z);
        var c = ox * ox + oz * oz - radius * radius;

        const float eps = 1e-8f;
        if (a < eps)
        {
            if (c > 0f)
                return false;

            return RangesOverlap(origin.Y, target.Y, ymin, ymax);
        }

        var disc = b * b - 4f * a * c;
        if (disc < 0f)
            return false;

        var inv2A = 0.5f / a;
        var sqrtDisc = MathF.Sqrt(disc);
        var tEnter = (-b - sqrtDisc) * inv2A;
        var tExit = (-b + sqrtDisc) * inv2A;
        if (tEnter > tExit)
            (tEnter, tExit) = (tExit, tEnter);

        var tMin = MathF.Max(tEnter, 0f);
        var tMax = MathF.Min(tExit, 1f);
        if (tMin > tMax)
            return false;

        return RangesOverlap(origin.Y + tMin * d.Y, origin.Y + tMax * d.Y, ymin, ymax);
    }

    private static bool RangesOverlap(float a, float b, float min, float max)
    {
        var lo = MathF.Min(a, b);
        var hi = MathF.Max(a, b);
        return hi >= min && lo <= max;
    }
}
