using System.Reflection;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;

namespace AISpace.Network.Tests;

public sealed class RoboMyProfilePacketTests
{
    [Fact]
    public void PacketTypes_UseDecompiledOpcodes()
    {
        Assert.Equal(0x99B4, (ushort)PacketType.EditRoboMyProfileRequest);
        Assert.Equal(0x2180, (ushort)PacketType.EditRoboMyProfileResponse);
        Assert.Equal(0x5AA9, (ushort)PacketType.GetMyRoboMyProfileDataRequest);
        Assert.Equal(0xFEAE, (ushort)PacketType.GetMyRoboMyProfileDataResponse);

        Assert.Equal(
            "send_edit_robo_myprofile",
            GetMetadata(PacketType.EditRoboMyProfileRequest).DecompiledName
        );
        Assert.Equal(
            "send_get_my_robo_myprofile_data",
            GetMetadata(PacketType.GetMyRoboMyProfileDataRequest).DecompiledName
        );
    }

    [Fact]
    public void GetMyRoboMyProfileDataRequest_ParsesRoboId()
    {
        var request = GetMyRoboMyProfileDataRequest.FromBytes([7, 0, 0, 0]);
        Assert.Equal(7u, request.RoboId);
        Assert.Throws<InvalidDataException>(() =>
            GetMyRoboMyProfileDataRequest.FromBytes([7, 0, 0])
        );
    }

    [Fact]
    public void EditRoboMyProfileRequest_ParsesRoboIdProfileAndJobId()
    {
        var metadata = new AvatarProfileMetadata(123, 0x11223344, 0x55667788);
        var profile = new ProfileData("a", "b", "c", "d1", "d2", "d3", "hello");
        var writer = new PacketWriter();
        writer.Write(3u);
        AvatarProfile.Write(writer, profile, metadata);
        writer.Write(42u);
        var payload = writer.ToBytes();

        Assert.Equal(0x507, payload.Length);
        var request = EditRoboMyProfileRequest.FromBytes(payload);
        Assert.Equal(3u, request.RoboId);
        Assert.Equal(42u, request.JobId);
        Assert.Equal(metadata, request.Metadata);
        Assert.Equal("a", request.Profile.Like1);
        Assert.Equal("hello", request.Profile.AvatarDesc);
    }

    [Fact]
    public void AvatarProfile_RoundTripsMetadataAndText()
    {
        var metadata = new AvatarProfileMetadata(123, 0x11223344, 0x55667788);
        var profile = new ProfileData("a", "b", "c", "d1", "d2", "d3", "hello");
        var writer = new PacketWriter();
        AvatarProfile.Write(writer, profile, metadata);
        var bytes = writer.ToBytes();

        Assert.Equal(0x4FF, AvatarProfile.WireSize);
        Assert.Equal(AvatarProfile.WireSize, bytes.Length);

        var metadataReader = new PacketReader(bytes);
        Assert.Equal(123u, metadataReader.ReadUInt());
        Assert.Equal(0x11223344u, metadataReader.ReadUInt());
        Assert.Equal(0x55667788u, metadataReader.ReadUInt());

        var reader = new PacketReader(bytes);
        var parsedProfile = AvatarProfile.Read(ref reader, out var parsedMetadata);
        Assert.Equal(metadata, parsedMetadata);
        Assert.Equal(profile, parsedProfile);
    }

    [Fact]
    public void AvatarMyProfilePackets_PreserveMetadata()
    {
        var metadata = new AvatarProfileMetadata(321, 0x12345678, 0x90ABCDEF);
        var profile = new ProfileData("a", "b", "c", "d1", "d2", "d3", "hello");
        var writer = new PacketWriter();
        AvatarProfile.Write(writer, profile, metadata);

        var request = MyProfileAvatarEditRequest.FromBytes(writer.ToBytes());
        Assert.Equal(metadata, request.Metadata);
        Assert.Equal(profile.Like1, request.Like1);
        Assert.Equal(profile.AvatarDesc, request.AvatarDesc);

        var responseBytes = new GetMyAvatarMyprofileDataResponse(profile, metadata).ToBytes();
        Assert.Equal(sizeof(uint) + AvatarProfile.WireSize, responseBytes.Length);
        var reader = new PacketReader(responseBytes);
        Assert.Equal(0u, reader.ReadUInt());
        var responseProfile = AvatarProfile.Read(ref reader, out var responseMetadata);
        Assert.Equal(metadata, responseMetadata);
        Assert.Equal(profile, responseProfile);
    }

    [Fact]
    public void GetAndEditResponses_UseExpectedLayouts()
    {
        Assert.Equal([0, 0, 0, 0], new EditRoboMyProfileResponse(0).ToBytes());

        var metadata = new AvatarProfileMetadata(123, 0x11223344, 0x55667788);
        var profile = new ProfileData("L1", "L2", "L3", "D1", "D2", "D3", "Desc");
        var bytes = new GetMyRoboMyProfileDataResponse(0, profile, metadata).ToBytes();
        Assert.Equal(0x503, bytes.Length);
        var reader = new PacketReader(bytes);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(123u, reader.ReadUInt());
        Assert.Equal(0x11223344u, reader.ReadUInt());
        Assert.Equal(0x55667788u, reader.ReadUInt());
        Assert.Equal("L1", reader.ReadFixedString(31, "Shift_JIS"));
        Assert.Equal("L2", reader.ReadFixedString(31, "Shift_JIS"));
        Assert.Equal("L3", reader.ReadFixedString(31, "Shift_JIS"));
        Assert.Equal("D1", reader.ReadFixedString(91, "Shift_JIS"));
        Assert.Equal("D2", reader.ReadFixedString(91, "Shift_JIS"));
        Assert.Equal("D3", reader.ReadFixedString(91, "Shift_JIS"));
        Assert.Equal("Desc", reader.ReadFixedString(901, "Shift_JIS"));
    }

    private static PacketMetadata GetMetadata(PacketType packetType)
    {
        var field = typeof(PacketType).GetField(packetType.ToString());
        return Assert.IsType<PacketMetadata>(field?.GetCustomAttribute<PacketMetadata>());
    }
}
