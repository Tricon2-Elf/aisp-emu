using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class NiconiCommonsBaseListRequest : IIncomingPacket<NiconiCommonsBaseListRequest>
{
    public static NiconiCommonsBaseListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }
}
