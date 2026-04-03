using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class CircleChatOutRequest : IIncomingPacket<CircleChatOutRequest>
{
    public static CircleChatOutRequest FromBytes(ReadOnlySpan<byte> data)
    {
        return new CircleChatOutRequest(); // Body is empty or contains ID, need to check by logs/code
    }
}
