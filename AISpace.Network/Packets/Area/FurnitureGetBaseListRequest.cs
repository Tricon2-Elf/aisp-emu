using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class FurnitureGetBaseListRequest : IPacket<FurnitureGetBaseListRequest>
{
    public static FurnitureGetBaseListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        throw new NotImplementedException();
    }
}
