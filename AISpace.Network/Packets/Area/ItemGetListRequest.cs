using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class ItemGetListRequest : IIncomingPacket<ItemGetListRequest>
{
    public static ItemGetListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }
}
