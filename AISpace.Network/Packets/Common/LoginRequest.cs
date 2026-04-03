namespace AISpace.Network.Packets.Common;

using AISpace.Network;

public class LoginRequest(uint Userid, ReadOnlySpan<byte> Otp) : IIncomingPacket<LoginRequest>
{
    public uint _userId = Userid;
    public byte[] _otp = Otp.ToArray();

    public static LoginRequest FromBytes(ReadOnlySpan<byte> data)
    {
        PacketReader reader = new(data);

        uint userid = reader.ReadUInt();
        ReadOnlySpan<byte> otp = reader.ReadBytes(20);
        return new LoginRequest(userid, otp);
    }
}
