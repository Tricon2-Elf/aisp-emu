using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;

namespace AISpace.Common.Tests;

public class MapLinkGeometryTests
{
    [Fact]
    public void GetTriggerLane_YawZero_CreatesHorizontalLaneThroughCenter()
    {
        var link = new MapLink
        {
            PositionX = -9800f,
            PositionY = 2f,
            PositionZ = -18000f,
            Yaw = 0,
            Length = 300f,
            Depth = 0f,
        };

        var lane = MapLinkGeometry.GetTriggerLane(link);

        Assert.Equal(-9500f, lane.StartX);
        Assert.Equal(-18000f, lane.StartZ);
        Assert.Equal(-10100f, lane.EndX);
        Assert.Equal(-18000f, lane.EndZ);
    }

    [Fact]
    public void DistanceSquaredToLane_IsZero_OnLane()
    {
        var link = new MapLink
        {
            PositionX = -9800f,
            PositionY = 2f,
            PositionZ = -18000f,
            Yaw = 0,
            Length = 300f,
            Depth = 0f,
        };

        var distanceSquared = MapLinkGeometry.DistanceSquaredToLane(link, -9800f, -18000f);

        Assert.Equal(0f, distanceSquared);
    }
}
