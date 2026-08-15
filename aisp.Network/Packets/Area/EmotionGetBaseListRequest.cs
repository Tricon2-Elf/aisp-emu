using aisp.Network;

namespace aisp.Network.Packets.Area;

public class EmotionGetBaseListRequest : IIncomingPacket<EmotionGetBaseListRequest>
{
    public static EmotionGetBaseListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }
}
