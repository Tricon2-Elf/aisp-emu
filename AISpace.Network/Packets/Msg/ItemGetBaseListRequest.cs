namespace AISpace.Network.Packets.Msg;

public class ItemGetBaseListRequest : IIncomingPacket<ItemGetBaseListRequest>
{
    public static ItemGetBaseListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }
}
