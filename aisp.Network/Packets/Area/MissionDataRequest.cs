using aisp.Network;

namespace aisp.Network.Packets.Area;

public class MissionDataRequest : IIncomingPacket<MissionDataRequest>
{
    public static MissionDataRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }
}
