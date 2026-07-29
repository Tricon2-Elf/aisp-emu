using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public sealed class GetAdventureWorkListRequest : IIncomingPacket<GetAdventureWorkListRequest>
{
    public static GetAdventureWorkListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        return new GetAdventureWorkListRequest();
    }
}
