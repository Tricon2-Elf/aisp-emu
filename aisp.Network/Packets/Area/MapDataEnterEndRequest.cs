using aisp.Network;

namespace aisp.Network.Packets.Area;

public class MapDataEnterEndRequest : IIncomingPacket<MapDataEnterEndRequest>
{
    public static MapDataEnterEndRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }
}
