using AISpace.Network;
using AISpace.Network.Data;
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

    [Theory]
    [InlineData(10990200u)]
    [InlineData(10990100u)]
    [InlineData(0u)]
    public void GetChannelListMapRequest_FromBytes(uint mapId)
    {
        var w = new PacketWriter();
        w.Write(mapId);
        var p = GetChannelListMapRequest.FromBytes(w.ToBytes());
        Assert.Equal(mapId, p.MapId);
    }

    [Fact]
    public void ChannelInfo_ToBytes_WritesFloatLoadField()
    {
        var bytes = new ChannelInfo(1, 4, 1000, new ServerInfo("localhost", 50054)).ToBytes();
        var reader = new PacketReader(bytes);

        Assert.Equal(ChannelInfo.PacketSize, bytes.Length);
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(4f, reader.ReadFloat());
        Assert.Equal(1000u, reader.ReadUInt());
        Assert.Equal((ushort)50054, reader.ReadUShort());
        Assert.Equal("localhost", reader.ReadFixedString(65, "ASCII"));
    }

    [Fact]
    public void NotifySelectMapData_ToBytes_WritesDirectRoutingEntries()
    {
        var packet = new NotifySelectMapData(
            new[]
            {
                new NotifySelectMapEntry
                {
                    MapId = 10990110,
                    AreaServerInfo = new ServerInfo("localhost", 50054),
                    ChannelId = 1,
                    RouteMapId = 10990110,
                    MapSerialId = 10990110,
                    RouteState = 0x12345678,
                    PositionX = -11000f,
                    PositionY = 0.1f,
                    PositionZ = -19200f,
                    Yaw = 90,
                    Animation = 0,
                    Unknown1 = 0x11111111,
                    Unknown2 = 0x22222222,
                },
                new NotifySelectMapEntry
                {
                    MapId = 10990200,
                    AreaServerInfo = new ServerInfo("192.168.0.10", 50055),
                    ChannelId = 2,
                    RouteMapId = 10990200,
                    MapSerialId = 10990200,
                    RouteState = 0,
                    PositionX = 1f,
                    PositionY = 2f,
                    PositionZ = 3f,
                    Yaw = 4,
                    Animation = 5,
                },
                new NotifySelectMapEntry
                {
                    MapId = 10990210,
                    AreaServerInfo = new ServerInfo("10.0.0.5", 50056),
                    ChannelId = 3,
                    RouteMapId = 10990210,
                    MapSerialId = 10990210,
                    PositionX = 9f,
                    PositionY = 8f,
                    PositionZ = 7f,
                    Yaw = 6,
                    Animation = 5,
                },
            }
        );

        var bytes = packet.ToBytes();
        var reader = new PacketReader(bytes);

        Assert.Equal(3u, reader.ReadUInt());

        var first = NotifySelectMapEntry.FromBytes(reader.ReadBytes(NotifySelectMapEntry.PacketSize));
        Assert.Equal(10990110u, first.MapId);
        Assert.Equal((ushort)50054, first.AreaServerInfo.Port);
        Assert.Equal("localhost", first.AreaServerInfo.IP);
        Assert.Equal(1u, first.ChannelId);
        Assert.Equal(10990110u, first.RouteMapId);
        Assert.Equal(10990110u, first.MapSerialId);
        Assert.Equal(0x12345678u, first.RouteState);
        Assert.Equal(-11000f, first.PositionX);
        Assert.Equal(0.1f, first.PositionY);
        Assert.Equal(-19200f, first.PositionZ);
        Assert.Equal((byte)90, first.Yaw);
        Assert.Equal((byte)0, first.Animation);
        Assert.Equal(0x11111111u, first.Unknown1);
        Assert.Equal(0x22222222u, first.Unknown2);

        var second = NotifySelectMapEntry.FromBytes(reader.ReadBytes(NotifySelectMapEntry.PacketSize));
        Assert.Equal(10990200u, second.MapId);
        Assert.Equal((ushort)50055, second.AreaServerInfo.Port);
        Assert.Equal("192.168.0.10", second.AreaServerInfo.IP);
        Assert.Equal(2u, second.ChannelId);
        Assert.Equal(10990200u, second.RouteMapId);
        Assert.Equal(10990200u, second.MapSerialId);
        Assert.Equal(1f, second.PositionX);
        Assert.Equal(2f, second.PositionY);
        Assert.Equal(3f, second.PositionZ);
        Assert.Equal((byte)4, second.Yaw);
        Assert.Equal((byte)5, second.Animation);

        var third = NotifySelectMapEntry.FromBytes(reader.ReadBytes(NotifySelectMapEntry.PacketSize));
        Assert.Equal(10990210u, third.MapId);
        Assert.Equal((ushort)50056, third.AreaServerInfo.Port);
        Assert.Equal("10.0.0.5", third.AreaServerInfo.IP);
        Assert.Equal(3u, third.ChannelId);
        Assert.Equal(10990210u, third.RouteMapId);
        Assert.Equal(10990210u, third.MapSerialId);
        Assert.Equal(9f, third.PositionX);
        Assert.Equal(8f, third.PositionY);
        Assert.Equal(7f, third.PositionZ);
        Assert.Equal((byte)6, third.Yaw);
        Assert.Equal((byte)5, third.Animation);
        Assert.Equal(4 + (NotifySelectMapEntry.PacketSize * 3), bytes.Length);
    }

    [Fact]
    public void NotifyChangeMap_ToBytes_WritesDirectRoutePayload()
    {
        var packet = new NotifyChangeMap
        {
            ChannelId = 1,
            MapId = 10990110,
            MapSerialId = 10990110,
            RouteState = 0x12345678,
            PositionX = -11000f,
            PositionY = 0.1f,
            PositionZ = -19200f,
            Rotation = -12,
            Animation = (byte)MovementType.Stopped,
            Flag = 0,
            AreaServerInfo = new ServerInfo("localhost", 50054),
            FadeFlag = 0,
        };

        var bytes = packet.ToBytes();
        var parsed = NotifyChangeMap.FromBytes(bytes);

        Assert.Equal(NotifyChangeMap.PacketSize, bytes.Length);
        Assert.Equal(1u, parsed.ChannelId);
        Assert.Equal(10990110u, parsed.MapId);
        Assert.Equal(10990110u, parsed.MapSerialId);
        Assert.Equal(0x12345678u, parsed.RouteState);
        Assert.Equal(-11000f, parsed.PositionX);
        Assert.Equal(0.1f, parsed.PositionY);
        Assert.Equal(-19200f, parsed.PositionZ);
        Assert.Equal((sbyte)-12, parsed.Rotation);
        Assert.Equal((byte)MovementType.Stopped, parsed.Animation);
        Assert.Equal((byte)0, parsed.Flag);
        Assert.Equal((ushort)50054, parsed.AreaServerInfo.Port);
        Assert.Equal("localhost", parsed.AreaServerInfo.IP);
        Assert.Equal((byte)0, parsed.FadeFlag);
    }

    [Fact]
    public void EventAreaMapSelectExecNotify_ToBytes_WritesSelectionEntriesAndFlags()
    {
        var packet = new EventAreaMapSelectExecNotify
        {
            Entries =
            [
                new NotifySelectMapEntry
                {
                    MapId = 10990110,
                    AreaServerInfo = new ServerInfo("localhost", 50054),
                    ChannelId = 1,
                    RouteMapId = 10990110,
                    MapSerialId = 10990110,
                    PositionX = -11000f,
                    PositionY = 0.1f,
                    PositionZ = -19200f,
                    Yaw = 0,
                    Animation = 0,
                },
                new NotifySelectMapEntry
                {
                    MapId = 10990200,
                    AreaServerInfo = new ServerInfo("localhost", 50054),
                    ChannelId = 1,
                    RouteMapId = 10990200,
                    MapSerialId = 10990200,
                    PositionX = -9600f,
                    PositionY = 0.1f,
                    PositionZ = -8400f,
                    Yaw = 45,
                    Animation = 0,
                },
                new NotifySelectMapEntry
                {
                    MapId = 10990210,
                    AreaServerInfo = new ServerInfo("localhost", 50054),
                    ChannelId = 1,
                    RouteMapId = 10990210,
                    MapSerialId = 10990210,
                    PositionX = -9600f,
                    PositionY = 0.1f,
                    PositionZ = -8800f,
                    Yaw = 90,
                    Animation = 0,
                },
            ],
            IslandId = 1u,
            IsRegisteredIsland = 0,
        };

        var parsed = EventAreaMapSelectExecNotify.FromBytes(packet.ToBytes());

        Assert.Equal([10990110u, 10990200u, 10990210u], parsed.MapIds);
        Assert.Equal(1u, parsed.IslandId);
        Assert.Equal(0u, parsed.IsRegisteredIsland);
        Assert.Collection(
            parsed.Entries,
            entry =>
            {
                Assert.Equal(10990110u, entry.MapId);
                Assert.Equal((ushort)50054, entry.AreaServerInfo.Port);
                Assert.Equal("localhost", entry.AreaServerInfo.IP);
                Assert.Equal(1u, entry.ChannelId);
                Assert.Equal(10990110u, entry.RouteMapId);
                Assert.Equal(10990110u, entry.MapSerialId);
            },
            entry =>
            {
                Assert.Equal(10990200u, entry.MapId);
                Assert.Equal((ushort)50054, entry.AreaServerInfo.Port);
                Assert.Equal("localhost", entry.AreaServerInfo.IP);
                Assert.Equal(1u, entry.ChannelId);
                Assert.Equal(10990200u, entry.RouteMapId);
                Assert.Equal(10990200u, entry.MapSerialId);
            },
            entry =>
            {
                Assert.Equal(10990210u, entry.MapId);
                Assert.Equal((ushort)50054, entry.AreaServerInfo.Port);
                Assert.Equal("localhost", entry.AreaServerInfo.IP);
                Assert.Equal(1u, entry.ChannelId);
                Assert.Equal(10990210u, entry.RouteMapId);
                Assert.Equal(10990210u, entry.MapSerialId);
            }
        );
        Assert.Equal(4 + (NotifySelectMapEntry.PacketSize * 3) + 8, packet.ToBytes().Length);
    }

    [Fact]
    public void EventAreaMapSelectExecRRequest_FromBytes_ReadsResultMapAndChannel()
    {
        var packet = new EventAreaMapSelectExecRRequest
        {
            Result = 0,
            MapId = 10990200,
            ChannelId = 1,
        };

        var parsed = EventAreaMapSelectExecRRequest.FromBytes(packet.ToBytes());

        Assert.Equal(0u, parsed.Result);
        Assert.Equal(10990200u, parsed.MapId);
        Assert.Equal(1u, parsed.ChannelId);
    }

    [Fact]
    public void EventAreaMapSelectCloseNotify_ToBytes_WritesResult()
    {
        var parsed = EventAreaMapSelectCloseNotify.FromBytes(new EventAreaMapSelectCloseNotify(1).ToBytes());
        Assert.Equal(1u, parsed.Result);
    }

    [Fact]
    public void SelectInitIslandStartNotify_ToBytes_WritesIslandBootstrapEntries()
    {
        var packet = new SelectInitIslandStartNotify
        {
            Islands =
            [
                new SelectInitIslandEntry
                {
                    IslandId = 1,
                    Title = "Akihabara Island 1",
                    Description = "Akihabara 2",
                },
                new SelectInitIslandEntry
                {
                    IslandId = 2,
                    Title = "Akihabara Island 2",
                    Description = "Akihabara 3\nAkihabara 4",
                },
            ],
        };

        var parsed = SelectInitIslandStartNotify.FromBytes(packet.ToBytes());

        Assert.Collection(
            parsed.Islands,
            island =>
            {
                Assert.Equal(1u, island.IslandId);
                Assert.Equal("Akihabara Island 1", island.Title);
                Assert.Equal("Akihabara 2", island.Description);
            },
            island =>
            {
                Assert.Equal(2u, island.IslandId);
                Assert.Equal("Akihabara Island 2", island.Title);
                Assert.Equal("Akihabara 3\nAkihabara 4", island.Description);
            }
        );
        Assert.Equal(4 + (SelectInitIslandEntry.PacketSize * 2), packet.ToBytes().Length);
    }

    [Fact]
    public void SelectInitIslandEndRequest_FromBytes_ReadsIslandId()
    {
        var packet = new SelectInitIslandEndRequest { IslandId = 3 };

        var parsed = SelectInitIslandEndRequest.FromBytes(packet.ToBytes());

        Assert.Equal(3u, parsed.IslandId);
    }
}
