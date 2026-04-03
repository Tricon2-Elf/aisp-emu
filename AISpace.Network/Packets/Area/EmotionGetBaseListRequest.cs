using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class EmotionGetBaseListRequest : IIncomingPacket<EmotionGetBaseListRequest>
{
    public static EmotionGetBaseListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }
}
