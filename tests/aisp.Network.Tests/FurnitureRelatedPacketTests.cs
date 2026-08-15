using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Network.Tests;

public class FurnitureRelatedPacketTests
{
    [Theory]
    [InlineData(PacketType.RoboFurnactStartRequest, 0x08F2, "send_robo_furnact_start")]
    [InlineData(PacketType.RoboFurnactEndRequest, 0xE7BC, "send_robo_furnact_end")]
    [InlineData(PacketType.NotifyRoboFurnactStart, 0xB77E, "recv_notify_robo_furnact_start")]
    [InlineData(PacketType.NotifyRoboFurnactEnd, 0xB45C, "recv_notify_robo_furnact_end")]
    [InlineData(PacketType.StorageFurnOpenResponse, 0x88C1, "recv_storage_furn_open_r")]
    [InlineData(PacketType.StorageFurnCloseResponse, 0x4E60, "recv_storage_furn_close_r")]
    [InlineData(PacketType.MyRoomThrowoutOthersResponse, 0xB05A, "recv_myroom_throwout_others_r")]
    [InlineData(
        PacketType.NotifyMyHouseChangeSecurity,
        0x8F88,
        "recv_notify_myhouse_change_security"
    )]
    [InlineData(
        PacketType.NotifyMissionPartyListOpenStart,
        0x878B,
        "recv_notify_mission_party_list_open_start"
    )]
    public void PacketType_UsesExpectedOpcode(
        PacketType packetType,
        ushort opcode,
        string decompiledName
    )
    {
        Assert.Equal(opcode, (ushort)packetType);
        var field = typeof(PacketType).GetField(packetType.ToString());
        var metadata = Assert.IsType<PacketMetadata>(
            field?.GetCustomAttributes(typeof(PacketMetadata), false).SingleOrDefault()
        );
        Assert.Equal(decompiledName, metadata.DecompiledName);
        if (packetType != PacketType.NotifyMissionPartyListOpenStart)
            Assert.Equal(ImplementationState.Implemented, metadata.State);
    }

    [Fact]
    public void NotifyMissionPartyListOpenStart_DoesNotCollideWithStorageFurnOpen()
    {
        Assert.NotEqual(
            (ushort)PacketType.NotifyMissionPartyListOpenStart,
            (ushort)PacketType.StorageFurnOpenResponse
        );
    }

    [Fact]
    public void RoboFurnactStartRequest_RoundTripsMovement()
    {
        var start = new MovementData(1f, 2f, 3f, 45, MovementType.Walking);
        var writer = new PacketWriter();
        writer.Write(9u);
        writer.Write(11u);
        writer.Write(start.ToBytes());
        var parsed = RoboFurnactStartRequest.FromBytes(writer.ToBytes());
        Assert.Equal(9u, parsed.RoboId);
        Assert.Equal(11u, parsed.FurnitureId);
        Assert.Equal(start.X, parsed.Start.X);
        Assert.Equal(start.Animation, parsed.Start.Animation);
    }

    [Fact]
    public void NotifyMyHouseChangeSecurity_SerializesEightBytes()
    {
        var bytes = new NotifyMyHouseChangeSecurity(42, MyRoomSecurity.FriendsOnly).ToBytes();
        Assert.Equal(8, bytes.Length);
        var reader = new PacketReader(bytes);
        Assert.Equal(42u, reader.ReadUInt());
        Assert.Equal((uint)MyRoomSecurity.FriendsOnly, reader.ReadUInt());
    }
}
