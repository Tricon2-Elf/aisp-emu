namespace aisp.Network.Data;

/// <summary>
/// Metadata preceding the editable text in a native <c>AvatarProfileDesc</c>.
/// The client displays <see cref="PlayDurationDays"/> as the profile's play period. The other
/// two DWORDs are not interpreted by this client build, but are preserved and echoed on edit.
/// </summary>
public readonly record struct AvatarProfileMetadata(
    uint PlayDurationDays,
    uint UnknownDword04,
    uint UnknownDword08
);

/// <summary>
/// Wire layout for <c>AvatarProfileDesc</c> / myprofile blocks used by avatar and Robo profile packets:
/// play-duration days, two opaque pass-through DWORDs, 3×31 likes, 3×91 like descriptions,
/// and a 901-byte UTF-8 description. The serialized size is 0x4FF bytes; the native 0x500-byte
/// structure has one trailing alignment byte that is not transmitted.
/// </summary>
public static class AvatarProfile
{
    public const int MetadataBytes = sizeof(uint) * 3;
    public const int WireSize = MetadataBytes + (31 * 3) + (91 * 3) + 901;

    public static ProfileData Read(ref PacketReader reader) => Read(ref reader, out _);

    public static ProfileData Read(ref PacketReader reader, out AvatarProfileMetadata metadata)
    {
        metadata = new AvatarProfileMetadata(
            reader.ReadUInt(),
            reader.ReadUInt(),
            reader.ReadUInt()
        );
        var like1 = reader.ReadFixedString(31);
        var like2 = reader.ReadFixedString(31);
        var like3 = reader.ReadFixedString(31);
        var likeDesc1 = reader.ReadFixedString(91);
        var likeDesc2 = reader.ReadFixedString(91);
        var likeDesc3 = reader.ReadFixedString(91);
        var description = reader.ReadFixedString(901);
        return new ProfileData(like1, like2, like3, likeDesc1, likeDesc2, likeDesc3, description);
    }

    public static void Write(PacketWriter writer, ProfileData profile) =>
        Write(writer, profile, default);

    public static void Write(
        PacketWriter writer,
        ProfileData profile,
        AvatarProfileMetadata metadata
    )
    {
        writer.Write(metadata.PlayDurationDays);
        writer.Write(metadata.UnknownDword04);
        writer.Write(metadata.UnknownDword08);
        writer.WriteFixedString(profile.Like1, 31);
        writer.WriteFixedString(profile.Like2, 31);
        writer.WriteFixedString(profile.Like3, 31);
        writer.WriteFixedString(profile.LikeDesc1, 91);
        writer.WriteFixedString(profile.LikeDesc2, 91);
        writer.WriteFixedString(profile.LikeDesc3, 91);
        writer.WriteFixedString(profile.AvatarDesc, 901);
    }
}
