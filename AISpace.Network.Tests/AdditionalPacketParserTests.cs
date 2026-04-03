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

    [Theory]
    [InlineData(10990110u, 1u)]
    [InlineData(0u, 0u)]
    public void MapEnterRequest_FromBytes(uint mapId, uint channelId)
    {
        var w = new PacketWriter();
        w.Write(mapId);
        w.Write(channelId);
        var p = AreaMapEnterRequest.FromBytes(w.ToBytes());
        Assert.Equal(mapId, p.MapID);
        Assert.Equal(channelId, p.ChannelId);
    }

    [Fact]
    public void NotifySelectMapData_ToBytes_WritesOrderedMapIdsWithExpectedStride()
    {
        var packet = new NotifySelectMapData(new uint[] { 10990110, 10990200, 10990210 });

        var bytes = packet.ToBytes();
        var reader = new PacketReader(bytes);

        Assert.Equal(3u, reader.ReadUInt());

        Assert.Equal(10990110u, reader.ReadUInt());
        reader.ReadBytes(105);
        Assert.Equal(10990200u, reader.ReadUInt());
        reader.ReadBytes(105);
        Assert.Equal(10990210u, reader.ReadUInt());
        reader.ReadBytes(105);
        Assert.Equal(4 + (109 * 3), bytes.Length);
    }
}
