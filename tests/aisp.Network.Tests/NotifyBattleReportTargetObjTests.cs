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

    [Fact]
    public void TargetPos_ToBytes_MatchesClientReportPlusVec3Layout()
    {
        var packet = new NotifyBattleReportTargetPos(
            attackerObjectId: 10,
            actionType: 5,
            actionFlags: 0,
            skillId: 200090,
            targetPos: new System.Numerics.Vector3(-9200f, 0.1f, -14285f)
        );

        var bytes = packet.ToBytes();
        var reader = new PacketReader(bytes);

        Assert.Equal(29, bytes.Length);
        Assert.Equal(10u, reader.ReadUInt());
        Assert.Equal(5u, reader.ReadUInt());
        Assert.Equal((byte)0, reader.ReadByte());
        Assert.Equal(200090u, reader.ReadUInt());
        Assert.Equal(-9200f, reader.ReadFloat());
        Assert.Equal(0.1f, reader.ReadFloat());
        Assert.Equal(-14285f, reader.ReadFloat());
        Assert.Equal(0u, reader.ReadUInt());
    }
}
