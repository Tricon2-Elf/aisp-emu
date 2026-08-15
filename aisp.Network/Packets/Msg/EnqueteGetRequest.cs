using aisp.Network;

namespace aisp.Network.Packets.Msg;

public class EnqueteGetRequest : IIncomingPacket<EnqueteGetRequest>
{
    public static EnqueteGetRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }
}
