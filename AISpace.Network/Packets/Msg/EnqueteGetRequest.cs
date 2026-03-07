using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class EnqueteGetRequest : IPacket<EnqueteGetRequest>
{
    public static EnqueteGetRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        throw new NotImplementedException();
    }
}
