using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class LogoutRequest : IPacket<LogoutRequest>
{
    public static LogoutRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        throw new NotImplementedException();
    }
}
