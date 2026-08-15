using aisp.Network;

namespace aisp.Network.Packets.Area;

public sealed class GetAdventureWorkListRequest : IIncomingPacket<GetAdventureWorkListRequest>
{
    public static GetAdventureWorkListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        return new GetAdventureWorkListRequest();
    }
}
