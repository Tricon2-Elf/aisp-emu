namespace AISpace.Common.Network.Packets.Msg;

public class CircleChatOutRequest : IPacket<CircleChatOutRequest>
{
    public static CircleChatOutRequest FromBytes(ReadOnlySpan<byte> data)
    {
        return new CircleChatOutRequest(); // Body is empty or contains ID, need to check by logs/code
    }

    public byte[] ToBytes() => [];
}
