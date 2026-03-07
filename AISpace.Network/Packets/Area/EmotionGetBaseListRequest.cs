using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class EmotionGetBaseListRequest : IPacket<EmotionGetBaseListRequest>
{
    public static EmotionGetBaseListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        throw new NotImplementedException();
    }
}
