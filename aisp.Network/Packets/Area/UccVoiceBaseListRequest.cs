using aisp.Network;

namespace aisp.Network.Packets.Area;

public class UccVoiceBaseListRequest : IIncomingPacket<UccVoiceBaseListRequest>
{
    public static UccVoiceBaseListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }
}
