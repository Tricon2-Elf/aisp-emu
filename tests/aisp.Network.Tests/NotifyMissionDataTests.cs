using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Network.Tests;

public sealed class NotifyMissionDataTests
{
    [Fact]
    public void WireLayout_MatchesClientMissionReader()
    {
        var bytes = new NotifyMissionData(
            timeLimitSeconds: 1800,
            missionRuleType: MissionRuleType.FreeAim
        ).ToBytes();
        Assert.Equal(NotifyMissionData.WireSize, bytes.Length);

        var reader = new PacketReader(bytes);
        Assert.Equal(603u, reader.ReadUInt());
        reader.ReadBytes(49);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(1800u, reader.ReadUInt());
        reader.ReadBytes(361);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(30u, reader.ReadUInt());
    }
}
