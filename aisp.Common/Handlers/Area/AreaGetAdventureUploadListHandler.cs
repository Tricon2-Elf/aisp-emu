using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public sealed class AreaGetAdventureUploadListHandler
    : PacketHandlerBase<GetAdventureUploadListRequest, GetAdventureUploadListResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.GetAdventureUploadListRequest;
    public override PacketType ResponseType => PacketType.GetAdventureUploadListResponse;
    public override ServerType ServerType => ServerType.Area;

    public override Task<GetAdventureUploadListResponse?> HandleAsync(
        GetAdventureUploadListRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    ) => Task.FromResult<GetAdventureUploadListResponse?>(new GetAdventureUploadListResponse());
}
