using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class EnqueteGetRequest : IIncomingPacket<EnqueteGetRequest>
{
    public static EnqueteGetRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }
}
