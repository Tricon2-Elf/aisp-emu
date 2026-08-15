using aisp.Network;

namespace aisp.Network.Packets.Common;

public class LoginResponse(AuthResponseResult Result) : IOutgoingPacket
{
    //Result: 0 = Success
    //Any other value = Failure
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)Result);
        return writer.ToBytes();
    }
}
