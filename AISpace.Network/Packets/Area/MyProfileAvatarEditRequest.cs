using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class MyProfileAvatarEditRequest(
    string like1,
    string like2,
    string like3,
    string likeDesc1,
    string likeDesc2,
    string likeDesc3,
    string avatarDesc
) : IIncomingPacket<MyProfileAvatarEditRequest>
{
    public string Like1 { get; } = like1;
    public string Like2 { get; } = like2;
    public string Like3 { get; } = like3;
    public string LikeDesc1 { get; } = likeDesc1;
    public string LikeDesc2 { get; } = likeDesc2;
    public string LikeDesc3 { get; } = likeDesc3;
    public string AvatarDesc { get; } = avatarDesc;

    public static MyProfileAvatarEditRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        reader.ReadBytes(12);
        var l1 = reader.ReadFixedString(31, "Shift_JIS");
        var l2 = reader.ReadFixedString(31, "Shift_JIS");
        var l3 = reader.ReadFixedString(31, "Shift_JIS");
        var d1 = reader.ReadFixedString(91, "Shift_JIS");
        var d2 = reader.ReadFixedString(91, "Shift_JIS");
        var d3 = reader.ReadFixedString(91, "Shift_JIS");
        var desc = reader.ReadFixedString(901, "Shift_JIS");
        return new MyProfileAvatarEditRequest(l1, l2, l3, d1, d2, d3, desc);
    }
}
