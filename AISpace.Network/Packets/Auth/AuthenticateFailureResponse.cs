using AISpace.Network;

namespace AISpace.Network.Packets.Auth;

public class AuthenticateFailureResponse(AuthResponseResult Result) : IPacket<AuthenticateFailureResponse>
{
    public static AuthenticateFailureResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)Result);
        return writer.ToBytes();
    }
}
