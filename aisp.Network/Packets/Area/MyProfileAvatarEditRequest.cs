using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public class MyProfileAvatarEditRequest(
    string like1,
    string like2,
    string like3,
    string likeDesc1,
    string likeDesc2,
    string likeDesc3,
    string avatarDesc,
    AvatarProfileMetadata metadata = default
) : IIncomingPacket<MyProfileAvatarEditRequest>
{
    public string Like1 { get; } = like1;
    public string Like2 { get; } = like2;
    public string Like3 { get; } = like3;
    public string LikeDesc1 { get; } = likeDesc1;
    public string LikeDesc2 { get; } = likeDesc2;
    public string LikeDesc3 { get; } = likeDesc3;
    public string AvatarDesc { get; } = avatarDesc;
    public AvatarProfileMetadata Metadata { get; } = metadata;

    public static MyProfileAvatarEditRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var profile = AvatarProfile.Read(ref reader, out var metadata);
        return new MyProfileAvatarEditRequest(
            profile.Like1,
            profile.Like2,
            profile.Like3,
            profile.LikeDesc1,
            profile.LikeDesc2,
            profile.LikeDesc3,
            profile.AvatarDesc,
            metadata
        );
    }
}
