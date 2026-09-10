using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Network.Tests;

public class NotifyBattleReportTargetObjTests
{
    [Fact]
    public void ToBytes_MatchesClientReportPlusTargetLayout()
    {
        var packet = new NotifyBattleReportTargetObj(
            attackerObjectId: 10,
            actionType: 4,
            actionFlags: 0,
            skillId: 200090,
            targetObjectId: 2_000_001
        );

        var bytes = packet.ToBytes();
        var reader = new PacketReader(bytes);

        Assert.Equal(21, bytes.Length);
        Assert.Equal(10u, reader.ReadUInt());
        Assert.Equal(4u, reader.ReadUInt());
        Assert.Equal((byte)0, reader.ReadByte());
        Assert.Equal(200090u, reader.ReadUInt());
        Assert.Equal(2_000_001u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());
    }
}
