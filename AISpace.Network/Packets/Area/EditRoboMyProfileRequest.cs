using AISpace.Network.Data;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// send_edit_robo_myprofile (0x99B4): roboid + AvatarProfileDesc + jobid.
/// Profile layout matches avatar myprofile (play duration, two opaque DWORDs, likes,
/// like descriptions, and description).
/// </summary>
public sealed class EditRoboMyProfileRequest(
    uint roboId,
    ProfileData profile,
    uint jobId,
    AvatarProfileMetadata metadata = default
) : IIncomingPacket<EditRoboMyProfileRequest>
{
    public uint RoboId { get; } = roboId;
    public ProfileData Profile { get; } = profile;
    public uint JobId { get; } = jobId;
    public AvatarProfileMetadata Metadata { get; } = metadata;

    public static EditRoboMyProfileRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var roboId = reader.ReadUInt();
        var profile = AvatarProfile.Read(ref reader, out var metadata);
        var jobId = reader.ReadUInt();
        return new EditRoboMyProfileRequest(roboId, profile, jobId, metadata);
    }
}
