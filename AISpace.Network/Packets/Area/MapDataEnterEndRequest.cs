using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class MapDataEnterEndRequest : IIncomingPacket<MapDataEnterEndRequest>
{
    public static MapDataEnterEndRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }
}
