using aisp.Network;

namespace aisp.Network.Packets.Auth;

public class AuthenticateFailureResponse(AuthResponseResult Result) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)Result);
        return writer.ToBytes();
    }
}
