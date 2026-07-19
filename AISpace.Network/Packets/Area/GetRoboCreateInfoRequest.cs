namespace AISpace.Network.Packets.Area;

public class GetRoboCreateInfoRequest : IIncomingPacket<GetRoboCreateInfoRequest>
{
    public static GetRoboCreateInfoRequest FromBytes(ReadOnlySpan<byte> data) => new();
}
