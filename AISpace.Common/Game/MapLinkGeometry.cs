using AISpace.Common.DAL.Entities;

namespace AISpace.Common.Game;

public static class MapLinkGeometry
{
    public static TriggerLane GetTriggerLane(MapLink link)
    {
        var angle = link.Yaw * (MathF.PI / 180f);

        // Decompiled recv_notify_maplink_data handling builds the lane by first
        // shifting the center along one axis (float_14) and then extending a line
        // along the perpendicular axis (float_10).
        var offsetX = MathF.Sin(angle) * link.Depth;
        var offsetZ = MathF.Cos(angle) * link.Depth;
        var halfLineX = MathF.Cos(angle) * link.Length;
        var halfLineZ = -MathF.Sin(angle) * link.Length;

        var centerX = link.PositionX + offsetX;
        var centerZ = link.PositionZ + offsetZ;

        return new TriggerLane(StartX: centerX + halfLineX, StartZ: centerZ + halfLineZ, EndX: centerX - halfLineX, EndZ: centerZ - halfLineZ);
    }

    public static TriggerRectangle GetTriggerRectangle(MapLink link)
    {
        var angle = link.Yaw * (MathF.PI / 180f);
        var tangentX = MathF.Cos(angle);
        var tangentZ = -MathF.Sin(angle);
        var normalX = MathF.Sin(angle);
        var normalZ = MathF.Cos(angle);

        // The raw maplink data models a lane whose base line starts at PositionX/Z
        // and extends `Depth` units along the normal.
        var halfDepth = MathF.Abs(link.Depth) * 0.5f;
        var centerX = link.PositionX + (normalX * (link.Depth * 0.5f));
        var centerZ = link.PositionZ + (normalZ * (link.Depth * 0.5f));

        return new TriggerRectangle(CenterX: centerX, CenterZ: centerZ, TangentX: tangentX, TangentZ: tangentZ, NormalX: normalX, NormalZ: normalZ, HalfLength: MathF.Abs(link.Length), HalfDepth: halfDepth);
    }

    public static bool ContainsPoint(MapLink link, float x, float z)
    {
        var rectangle = GetTriggerRectangle(link);
        var local = ToLocal(rectangle, x, z);
        return MathF.Abs(local.LocalTangent) <= rectangle.HalfLength && MathF.Abs(local.LocalNormal) <= rectangle.HalfDepth;
    }

    public static bool IntersectsSegment(MapLink link, float startX, float startZ, float endX, float endZ)
    {
        var rectangle = GetTriggerRectangle(link);
        var start = ToLocal(rectangle, startX, startZ);
        var end = ToLocal(rectangle, endX, endZ);
        return SegmentIntersectsAxisAlignedRectangle(start.LocalTangent, start.LocalNormal, end.LocalTangent, end.LocalNormal, rectangle.HalfLength, rectangle.HalfDepth);
    }

    public static float DistanceSquaredToRectangle(MapLink link, float x, float z)
    {
        var rectangle = GetTriggerRectangle(link);
        var local = ToLocal(rectangle, x, z);
        var deltaTangent = MathF.Max(MathF.Abs(local.LocalTangent) - rectangle.HalfLength, 0f);
        var deltaNormal = MathF.Max(MathF.Abs(local.LocalNormal) - rectangle.HalfDepth, 0f);
        return (deltaTangent * deltaTangent) + (deltaNormal * deltaNormal);
    }

    public static float DistanceSquaredToLane(MapLink link, float x, float z)
    {
        var lane = GetTriggerLane(link);
        var segmentX = lane.EndX - lane.StartX;
        var segmentZ = lane.EndZ - lane.StartZ;
        var segmentLengthSquared = (segmentX * segmentX) + (segmentZ * segmentZ);

        if (segmentLengthSquared <= 0.0001f)
        {
            var pointDeltaX = x - lane.StartX;
            var pointDeltaZ = z - lane.StartZ;
            return (pointDeltaX * pointDeltaX) + (pointDeltaZ * pointDeltaZ);
        }

        var projection = (((x - lane.StartX) * segmentX) + ((z - lane.StartZ) * segmentZ)) / segmentLengthSquared;
        var clampedProjection = Math.Clamp(projection, 0f, 1f);
        var closestX = lane.StartX + (segmentX * clampedProjection);
        var closestZ = lane.StartZ + (segmentZ * clampedProjection);
        var deltaX = x - closestX;
        var deltaZ = z - closestZ;
        return (deltaX * deltaX) + (deltaZ * deltaZ);
    }

    private static LocalPoint ToLocal(TriggerRectangle rectangle, float x, float z)
    {
        var deltaX = x - rectangle.CenterX;
        var deltaZ = z - rectangle.CenterZ;
        return new LocalPoint(LocalTangent: (deltaX * rectangle.TangentX) + (deltaZ * rectangle.TangentZ), LocalNormal: (deltaX * rectangle.NormalX) + (deltaZ * rectangle.NormalZ));
    }

    private static bool SegmentIntersectsAxisAlignedRectangle(float startTangent, float startNormal, float endTangent, float endNormal, float halfLength, float halfDepth)
    {
        if (MathF.Abs(startTangent) <= halfLength && MathF.Abs(startNormal) <= halfDepth)
            return true;

        if (MathF.Abs(endTangent) <= halfLength && MathF.Abs(endNormal) <= halfDepth)
            return true;

        var deltaTangent = endTangent - startTangent;
        var deltaNormal = endNormal - startNormal;
        var tMin = 0f;
        var tMax = 1f;

        return Clip(-deltaTangent, startTangent + halfLength, ref tMin, ref tMax) && Clip(deltaTangent, halfLength - startTangent, ref tMin, ref tMax) && Clip(-deltaNormal, startNormal + halfDepth, ref tMin, ref tMax) && Clip(deltaNormal, halfDepth - startNormal, ref tMin, ref tMax);
    }

    private static bool Clip(float p, float q, ref float tMin, ref float tMax)
    {
        if (MathF.Abs(p) <= 0.0001f)
            return q >= 0f;

        var t = q / p;
        if (p < 0f)
        {
            if (t > tMax)
                return false;

            if (t > tMin)
                tMin = t;
        }
        else
        {
            if (t < tMin)
                return false;

            if (t < tMax)
                tMax = t;
        }

        return true;
    }

    public readonly record struct TriggerLane(float StartX, float StartZ, float EndX, float EndZ);

    public readonly record struct TriggerRectangle(float CenterX, float CenterZ, float TangentX, float TangentZ, float NormalX, float NormalZ, float HalfLength, float HalfDepth);

    private readonly record struct LocalPoint(float LocalTangent, float LocalNormal);
}
