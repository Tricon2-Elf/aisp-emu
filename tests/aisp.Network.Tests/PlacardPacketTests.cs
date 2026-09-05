using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Network.Tests;

public sealed class PlacardPacketTests
{
    [Fact]
    public void SettingResponse_ContainsTheRequiredFixedPlacardRecord()
    {
        Assert.Equal(131, new PlacardSettingResponse(1).ToBytes().Length);
    }

    [Fact]
    public void MapNotify_WritesAPlacardCollection()
    {
        Assert.Equal(131, new NotifyPlacardInMap(1, "Player", 2, 1, 0, 0).ToBytes().Length);
    }
}
