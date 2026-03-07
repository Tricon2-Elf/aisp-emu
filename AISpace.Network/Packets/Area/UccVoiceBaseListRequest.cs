using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class UccVoiceBaseListRequest : IPacket<UccVoiceBaseListRequest>
{
    public static UccVoiceBaseListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        throw new NotImplementedException();
    }
}
