using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public sealed class GetAdventureDownloadListRequest : IIncomingPacket<GetAdventureDownloadListRequest>
{
    public static GetAdventureDownloadListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        return new GetAdventureDownloadListRequest();
    }
}
