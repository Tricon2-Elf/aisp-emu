using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class ChannelListGetRequest : IPacket<ChannelListGetRequest>
{
    public static ChannelListGetRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        throw new NotImplementedException();
    }
}
