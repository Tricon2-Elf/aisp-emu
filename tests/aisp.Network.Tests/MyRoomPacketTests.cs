using System.Reflection;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using aisp.Network.Packets.Msg;

namespace aisp.Network.Tests;

public class MyRoomPacketTests
{
    [Theory]
    [InlineData(MyRoomSecurity.Private, 0u)]
    [InlineData(MyRoomSecurity.FriendsOnly, 1u)]
    [InlineData(MyRoomSecurity.CircleMembersOnly, 2u)]
    [InlineData(MyRoomSecurity.FriendsAndCircleMembers, 3u)]
    [InlineData(MyRoomSecurity.Public, 4u)]
    public void MyRoomSecurity_UsesClientValues(MyRoomSecurity security, uint wireValue)
    {
        Assert.Equal(wireValue, (uint)security);
    }

    public static TheoryData<PacketType, ushort, string> CorrectedPacketTypes =>
        new()
        {
            { PacketType.RoomListCloseRequest, 0x9A24, "send_room_list_close" },
            { PacketType.MyRoomUpdateNameRequest, 0xB154, "send_myroom_update_name" },
            {
                PacketType.NicotvGetInfoByFurnitureResponse,
                0x35A3,
                "recv_nicotv_get_info_by_furniture_r"
            },
            { PacketType.NotifyBgmPlay, 0x36C1, "recv_notify_bgm_play" },
            { PacketType.MyRoomUpdateSecurityRequest, 0xE54D, "send_myroom_update_security" },
            { PacketType.MyRoomSetFurnitureRequest, 0xAEFB, "send_myroom_set_furniture" },
            { PacketType.MyRoomRemoveFurnitureRequest, 0xD0DB, "send_myroom_remove_furniture" },
            { PacketType.MyRoomUpdateFurnitureRequest, 0x6405, "send_myroom_update_furniture" },
            { PacketType.NotifyRoomListOpenEnd, 0xDC32, "recv_notify_room_list_open_end" },
            {
                PacketType.NotifyMyRoomRemoveFurniture,
                0x7A75,
                "recv_notify_myroom_remove_furniture"
            },
            { PacketType.MyRoomSetFurnitureResponse, 0x1840, "recv_myroom_set_furniture_r" },
        };

    [Theory]
    [MemberData(nameof(CorrectedPacketTypes))]
    public void PacketType_UsesClientOpcodeAndSymbol(
        PacketType packetType,
        ushort opcode,
        string decompiledName
    )
    {
        Assert.Equal(opcode, (ushort)packetType);

        var field = typeof(PacketType).GetField(packetType.ToString());
        var metadata = Assert.IsType<PacketMetadata>(field?.GetCustomAttribute<PacketMetadata>());
        Assert.Equal(decompiledName, metadata.DecompiledName);
    }

    [Fact]
    public void RelatedPacketMetadata_HasNoDuplicateDirectionalOpcodes()
    {
        var related = typeof(PacketType)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => new
            {
                Field = field,
                Metadata = field.GetCustomAttribute<PacketMetadata>(),
            })
            .Where(entry =>
                entry.Metadata is not null && IsMyRoomRelated(entry.Metadata.DecompiledName)
            )
            .Select(entry => new
            {
                entry.Field.Name,
                entry.Metadata!.Server,
                entry.Metadata.Direction,
                Opcode = (ushort)(PacketType)entry.Field.GetValue(null)!,
            })
            .ToArray();

        var duplicates = related
            .GroupBy(entry => (entry.Server, entry.Direction, entry.Opcode))
            .Where(group => group.Count() > 1)
            .ToArray();
        Assert.Empty(duplicates);
    }

    [Fact]
    public void FurnitureGetBaseListRequest_RequiresEmptyPayload()
    {
        Assert.NotNull(FurnitureGetBaseListRequest.FromBytes([]));
        Assert.Throws<InvalidDataException>(() => FurnitureGetBaseListRequest.FromBytes([0]));
    }

    [Fact]
    public void FurnitureGetBaseListResponse_WritesClientLayoutAndEnforcesLimit()
    {
        var payload = new FurnitureGetBaseListResponse(
            0,
            [
                new FurnitureBaseEntry(11_000_000, FurniturePlacementFlags.Floor, 7),
                new FurnitureBaseEntry(
                    11_001_020,
                    FurniturePlacementFlags.Wall | FurniturePlacementFlags.Ceiling,
                    9
                ),
            ]
        ).ToBytes();
        var reader = new PacketReader(payload);

        Assert.Equal(8 + 2 * 12, payload.Length);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(11_000_000u, reader.ReadUInt());
        Assert.Equal((uint)FurniturePlacementFlags.Floor, reader.ReadUInt());
        Assert.Equal(7u, reader.ReadUInt());
        Assert.Equal(11_001_020u, reader.ReadUInt());
        Assert.Equal(
            (uint)(FurniturePlacementFlags.Wall | FurniturePlacementFlags.Ceiling),
            reader.ReadUInt()
        );
        Assert.Equal(9u, reader.ReadUInt());

        var tooMany = Enumerable
            .Repeat(
                new FurnitureBaseEntry(1, FurniturePlacementFlags.Floor, 0),
                FurnitureGetBaseListResponse.MaximumEntryCount + 1
            )
            .ToArray();
        Assert.Throws<InvalidOperationException>(() =>
            new FurnitureGetBaseListResponse(0, tooMany).ToBytes()
        );
    }

    [Fact]
    public void MyRoomGetFurnitureRequest_ReadsMapAndChannel()
    {
        var writer = new PacketWriter();
        writer.Write(20_000_030u);
        writer.Write(3u);

        var request = MyRoomGetFurnitureRequest.FromBytes(writer.ToBytes());

        Assert.Equal(20_000_030u, request.MapId);
        Assert.Equal(3u, request.ChannelId);
        Assert.Throws<InvalidDataException>(() =>
            MyRoomGetFurnitureRequest.FromBytes(writer.ToBytes().AsSpan(0, 4))
        );
    }

    [Fact]
    public void MyRoomSettingsRequests_ParseExactClientLayouts()
    {
        var nameWriter = new PacketWriter();
        nameWriter.Write(42u);
        nameWriter.Write("テスト");
        var nameRequest = MyRoomUpdateNameRequest.FromBytes(nameWriter.ToBytes());
        Assert.Equal(42u, nameRequest.RoomId);
        Assert.Equal("テスト", nameRequest.Name);

        var securityWriter = new PacketWriter();
        securityWriter.Write(42u);
        securityWriter.Write(2u);
        var securityRequest = MyRoomUpdateSecurityRequest.FromBytes(securityWriter.ToBytes());
        Assert.Equal(42u, securityRequest.RoomId);
        Assert.Equal(MyRoomSecurity.CircleMembersOnly, securityRequest.Security);
    }

    [Fact]
    public void FurnitureEditingRequests_ParseExactClientLayouts()
    {
        var writer = new PacketWriter();
        writer.Write(42u);
        writer.Write(73u);
        writer.Write(1.5f);
        writer.Write(2.5f);
        writer.Write(3.5f);
        writer.Write((byte)45);
        writer.Write((byte)90);
        var payload = writer.ToBytes();

        var set = MyRoomSetFurnitureRequest.FromBytes(payload);
        Assert.Equal(42u, set.RoomId);
        Assert.Equal(73u, set.SerialId);
        Assert.Equal(new MyRoomFurnitureTransform(1.5f, 2.5f, 3.5f, 45, 90), set.Transform);

        var update = MyRoomUpdateFurnitureRequest.FromBytes(payload);
        Assert.Equal(42u, update.RoomId);
        Assert.Equal(73u, update.FurnitureId);
        Assert.Equal(set.Transform, update.Transform);
        Assert.Throws<InvalidDataException>(() =>
            MyRoomSetFurnitureRequest.FromBytes(payload.AsSpan(0, payload.Length - 1))
        );
    }

    [Fact]
    public void FurnitureEditingNotifications_MatchClientPayloadSizes()
    {
        var transform = new MyRoomFurnitureTransform(1f, 2f, 3f, 4, 5);

        Assert.Equal(22, new NotifyMyRoomUpdateFurniture(42, 73, transform).ToBytes().Length);
        Assert.Equal(8, new NotifyMyRoomRemoveFurniture(42, 73).ToBytes().Length);
        Assert.Equal(8, new NotifyMyRoomUseFurniture(42, 73).ToBytes().Length);
        Assert.Empty(new NotifyRoomListOpenStart().ToBytes());
        Assert.Empty(new NotifyRoomListOpenEnd().ToBytes());
        Assert.Equal(4, new RoomListCloseResponse(0).ToBytes().Length);
    }

    [Fact]
    public void RoomListPack_MatchesClientPayloadLayoutAndLimit()
    {
        var rooms = new[]
        {
            new RoomListEntry(42, "テスト部屋", "所有者", 0),
            new RoomListEntry(73, "Second room", "Owner", 3),
        };

        var payload = new NotifyRoomListPack(rooms).ToBytes();
        var reader = new PacketReader(payload);

        Assert.Equal(4 + rooms.Length * RoomListEntry.WireSize, payload.Length);
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(42u, reader.ReadUInt());
        Assert.Equal("テスト部屋", reader.ReadFixedString(RoomListEntry.RoomNameLength));
        Assert.Equal("所有者", reader.ReadFixedString(RoomListEntry.OwnerNameLength));
        Assert.Equal(0, reader.ReadByte());
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new NotifyRoomListPack(
                Enumerable.Repeat(rooms[0], NotifyRoomListPack.MaximumRooms + 1).ToArray()
            )
        );
    }

    [Fact]
    public void ExistingMyRoomTransferPackets_MatchClientWireSizes()
    {
        var server = new ServerInfo("127.0.0.1", 50054);
        var room = new MyRoomData(
            42,
            42,
            MyRoomStage.TwelveTatami,
            "テスト部屋",
            MyRoomSecurity.CircleMembersOnly
        );
        var channelResponse = new ChannelSelectMyRoomResponse(
            0,
            server,
            20_000_030,
            20_000_030,
            room
        );
        var changeNotify = new NotifyChangeMyRoom
        {
            ChannelId = 1,
            MapId = 20_000_030,
            MapSerialId = 20_000_030,
            RouteState = 0,
            PositionX = 1,
            PositionY = 2,
            PositionZ = 3,
            Rotation = 180,
            Animation = 0,
            Flag = 0,
            AreaServerInfo = server,
            Room = room,
            FadeFlag = 1,
        };

        Assert.Equal(67, server.ToBytes().Length);
        Assert.Equal(75, room.ToBytes().Length);
        Assert.Equal(154, channelResponse.ToBytes().Length);
        Assert.Equal(174, changeNotify.ToBytes().Length);
        Assert.Equal(
            MyRoomFurnitureData.WireSize,
            new MyRoomNotifyFurniture(new MyRoomFurnitureData(42, 1, 0, 7001, 1, 2, 3, 4, 5, 1))
                .ToBytes()
                .Length
        );
    }

    private static bool IsMyRoomRelated(string decompiledName) =>
        decompiledName.Contains("myroom", StringComparison.OrdinalIgnoreCase)
        || decompiledName.Contains("myhouse", StringComparison.OrdinalIgnoreCase)
        || decompiledName.Contains("furniture", StringComparison.OrdinalIgnoreCase)
        || decompiledName.Contains("room_list", StringComparison.OrdinalIgnoreCase);
}
