using AISpace.Network;
using AISpace.Network.Packets.Area;
using AISpace.Network.Packets.Common;
using AISpace.Network.Packets.Msg;

namespace AISpace.Network.Tests;

public class AdditionalPacketParserTests
{
    public static IEnumerable<object[]> VersionChecks => new[] { new object[] { 1u, 2u, 3u }, new object[] { 0u, 0u, uint.MaxValue } };

    [Theory]
    [MemberData(nameof(VersionChecks))]
    public void VersionCheckRequest_FromBytes(uint major, uint minor, uint version)
    {
        var w = new PacketWriter();
        w.Write(major);
        w.Write(minor);
        w.Write(version);
        var p = VersionCheckRequest.FromBytes(w.ToBytes());
        Assert.Equal(major, p.Major);
        Assert.Equal(minor, p.Minor);
        Assert.Equal(version, p.Version);
    }

    [Theory]
    [InlineData(0x11111111u, 0x22222222u)]
    [InlineData(0u, 0u)]
    public void CircleChatInRequest_FromBytes(uint circleId, uint unk)
    {
        var w = new PacketWriter();
        w.Write(circleId);
        w.Write(unk);
        var p = CircleChatInRequest.FromBytes(w.ToBytes());
        Assert.Equal(circleId, p.CircleId);
        Assert.Equal(unk, p.Unk);
    }

    [Theory]
    [InlineData(10990100u, 1u)]
    [InlineData(0u, 0u)]
    public void MapLinkGetDataRequest_FromBytes(uint mapId, uint channelId)
    {
        var w = new PacketWriter();
        w.Write(mapId);
        w.Write(channelId);
        var p = MapLinkGetDataRequest.FromBytes(w.ToBytes());
        Assert.Equal(mapId, p.MapId);
        Assert.Equal(channelId, p.ChannelId);
    }
}
