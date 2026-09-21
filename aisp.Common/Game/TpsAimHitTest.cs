using System.Numerics;

namespace aisp.Common.Game;

/// <summary>
/// Free-aim hit test against the prototype mob's Y-up collision cylinder.
/// The client's <c>target_pos</c> is already a world raycast (walls clip the segment);
/// this only checks whether that segment intersects the mob volume.
/// </summary>
public static class TpsAimHitTest
{
    public static bool SegmentHitsPrototypeMob(Vector3 origin, Vector3 target) =>
        SegmentHitsYCylinder(
            origin,
            target,
            new Vector3(
                TpsPrototypeConstants.MobSpawnX,
                TpsPrototypeConstants.MobSpawnY,
                TpsPrototypeConstants.MobSpawnZ
            ),
            TpsPrototypeConstants.MobCollisionRadius,
            TpsPrototypeConstants.MobTpsActionVerticalRange
        );

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
