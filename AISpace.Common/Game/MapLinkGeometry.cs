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

    public readonly record struct TriggerLane(float StartX, float StartZ, float EndX, float EndZ);
}
