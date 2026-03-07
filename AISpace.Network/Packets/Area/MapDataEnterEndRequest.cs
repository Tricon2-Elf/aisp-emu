using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class MapDataEnterEndRequest : IPacket<MapDataEnterEndRequest>
{
    public static MapDataEnterEndRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        throw new NotImplementedException();
    }
}
