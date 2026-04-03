using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class RoboGetListRequest : IIncomingPacket<RoboGetListRequest>
{
    public static RoboGetListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }
}
