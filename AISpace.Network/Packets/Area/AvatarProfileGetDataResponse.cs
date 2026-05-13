using AISpace.Network.Data;

namespace AISpace.Network.Packets.Area;

public class AvatarProfileGetDataResponse(uint result, uint targetObjectId, ProfileData? profile) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(targetObjectId);

        if (profile != null)
        {
            writer.WriteFixedJisString(profile.Like1 ?? "", 31);
            writer.WriteFixedJisString(profile.Like2 ?? "", 31);
            writer.WriteFixedJisString(profile.Like3 ?? "", 31);
            writer.WriteFixedJisString(profile.LikeDesc1 ?? "", 91);
            writer.WriteFixedJisString(profile.LikeDesc2 ?? "", 91);
            writer.WriteFixedJisString(profile.LikeDesc3 ?? "", 91);
            writer.WriteFixedJisString(profile.AvatarDesc ?? "", 901);
            writer.Write(new byte[5]);
        }
        else
        {
            writer.Write(new byte[1272]);
        }

        return writer.ToBytes();
    }
}
