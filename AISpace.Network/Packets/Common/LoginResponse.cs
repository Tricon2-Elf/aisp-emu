using AISpace.Network;

namespace AISpace.Network.Packets.Common;

public class LoginResponse(AuthResponseResult Result) : IPacket<LoginResponse>
{
    //Result: 0 = Success
    //Any other value = Failure
    public static LoginResponse FromBytes(ReadOnlySpan<byte> data)
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
