using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

/// <summary>recv_get_my_robo_myprofile_data_r (0xFEAE): result + AvatarProfileDesc.</summary>
public sealed class GetMyRoboMyProfileDataResponse(
    uint result,
    ProfileData profile,
    AvatarProfileMetadata metadata = default
) : IOutgoingPacket
{
    public uint Result { get; } = result;
    public ProfileData Profile { get; } = profile;
    public AvatarProfileMetadata Metadata { get; } = metadata;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        AvatarProfile.Write(writer, Profile, Metadata);
        return writer.ToBytes();
    }
}
