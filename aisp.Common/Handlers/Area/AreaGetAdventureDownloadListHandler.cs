using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public sealed class AreaGetAdventureDownloadListHandler
    : PacketHandlerBase<GetAdventureDownloadListRequest, GetAdventureDownloadListResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.GetAdventureDownloadListRequest;
    public override PacketType ResponseType => PacketType.GetAdventureDownloadListResponse;
    public override ServerType ServerType => ServerType.Area;

    public override Task<GetAdventureDownloadListResponse?> HandleAsync(
        GetAdventureDownloadListRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    ) => Task.FromResult<GetAdventureDownloadListResponse?>(new GetAdventureDownloadListResponse());
}
