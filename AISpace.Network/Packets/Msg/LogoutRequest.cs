using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class LogoutRequest : IIncomingPacket<LogoutRequest>
{
    public static LogoutRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }
}
