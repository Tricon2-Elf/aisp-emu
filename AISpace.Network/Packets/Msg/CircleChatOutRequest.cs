using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class CircleChatOutRequest : IIncomingPacket<CircleChatOutRequest>
{
    public static CircleChatOutRequest FromBytes(ReadOnlySpan<byte> data) => new();
}
