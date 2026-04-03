using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class ChannelListGetRequest : IIncomingPacket<ChannelListGetRequest>
{
    public static ChannelListGetRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }
}
