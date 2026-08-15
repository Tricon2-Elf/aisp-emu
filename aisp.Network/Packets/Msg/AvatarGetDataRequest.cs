using aisp.Network;

namespace aisp.Network.Packets.Msg;

public class AvatarGetDataRequest() : IIncomingPacket<AvatarGetDataRequest>
{
    public static AvatarGetDataRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }
}
