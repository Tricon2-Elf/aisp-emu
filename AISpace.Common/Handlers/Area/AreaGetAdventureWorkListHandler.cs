using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public sealed class AreaGetAdventureWorkListHandler
    : PacketHandlerBase<GetAdventureWorkListRequest, GetAdventureWorkListResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.GetAdventureWorkListRequest;
    public override PacketType ResponseType => PacketType.GetAdventureWorkListResponse;
    public override ServerType ServerType => ServerType.Area;

    public override Task<GetAdventureWorkListResponse?> HandleAsync(
        GetAdventureWorkListRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    ) => Task.FromResult<GetAdventureWorkListResponse?>(new GetAdventureWorkListResponse());
}
