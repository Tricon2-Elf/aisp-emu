using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class CircleTalkRequest : IIncomingPacket<CircleTalkRequest>
{
    public static CircleTalkRequest FromBytes(ReadOnlySpan<byte> data) => new();
}
