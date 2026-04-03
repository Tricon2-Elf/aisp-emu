using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class FurnitureGetBaseListRequest : IIncomingPacket<FurnitureGetBaseListRequest>
{
    public static FurnitureGetBaseListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }
}
