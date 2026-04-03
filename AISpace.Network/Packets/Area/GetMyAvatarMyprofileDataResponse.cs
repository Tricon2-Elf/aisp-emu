using AISpace.Network.Data;

namespace AISpace.Network.Packets.Area;

public class GetMyAvatarMyprofileDataResponse(ProfileData pData) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)0);

        writer.Write((uint)0); // dword_0 (Circle ID?)
        writer.Write((uint)0); // dword_4 (Title ID?)
        writer.Write((uint)0); // dword_8 (Rank?)
        writer.WriteFixedJisString(pData.Like1, 31);
        writer.WriteFixedJisString(pData.Like2, 31);
        writer.WriteFixedJisString(pData.Like3, 31);
        writer.WriteFixedJisString(pData.LikeDesc1, 91);
        writer.WriteFixedJisString(pData.LikeDesc2, 91);
        writer.WriteFixedJisString(pData.LikeDesc3, 91);
        writer.WriteFixedJisString(pData.AvatarDesc, 901);

        return writer.ToBytes();
    }
}
