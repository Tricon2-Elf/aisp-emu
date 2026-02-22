using AISpace.Common.DAL.Entities;
using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Area;

public class GetMyAvatarMyprofileDataResponse(Character cha) : IPacket<GetMyAvatarMyprofileDataResponse>
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)0); // Result (Success)
        
        // Согласно строкам 678518-678526:
        writer.Write((uint)0); // dword_0 (Circle ID?)
        writer.Write((uint)0); // dword_4 (Title ID?)
        writer.Write((uint)0); // dword_8 (Rank?)

        // Интересы (Like 1, 2, 3) - по 31 байту (строка 678532)
        writer.WriteFixedJisString(cha.Like1, 31);
        writer.WriteFixedJisString(cha.Like2, 31);
        writer.WriteFixedJisString(cha.Like3, 31);

        // Описания интересов - по 91 байту (строка 678546)
        writer.WriteFixedJisString(cha.LikeDesc1, 91);
        writer.WriteFixedJisString(cha.LikeDesc2, 91);
        writer.WriteFixedJisString(cha.LikeDesc3, 91);

        // Общее описание профиля - 901 байт (строка 678557)
        writer.WriteFixedJisString(cha.AvatarDesc, 901);
        
        return writer.ToBytes();
    }

    public static GetMyAvatarMyprofileDataResponse FromBytes(ReadOnlySpan<byte> data) => throw new NotImplementedException();
}