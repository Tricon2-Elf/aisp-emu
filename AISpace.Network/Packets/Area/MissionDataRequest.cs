using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class MissionDataRequest : IIncomingPacket<MissionDataRequest>
{
    public static MissionDataRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }
}
