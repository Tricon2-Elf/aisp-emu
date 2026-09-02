using aisp.Network;

namespace aisp.Network.Packets.Area;

public sealed class GetAdventureUploadListRequest : IIncomingPacket<GetAdventureUploadListRequest>
{
    public static GetAdventureUploadListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        return new GetAdventureUploadListRequest();
    }
}
