using AISpace.Common.Game;
using AISpace.Common.Game.ServerScripts;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public sealed class AreaSelectInitIslandEndHandler(
    DirectMapLinkTransitionService directMapLinkTransitionService,
    ServerScriptDispatcher serverScriptDispatcher,
    ILogger<AreaSelectInitIslandEndHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.SelectInitIslandEndRequest;
    public PacketType ResponseType => PacketType.SelectInitIslandStart;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = SelectInitIslandEndRequest.FromBytes(payload.Span);
        logger.LogInformation("SelectInitIslandEndRequest from user {UserId}: island {IslandId}", session.User?.Id ?? session.UserId, request.IslandId);

        if (await serverScriptDispatcher.TryHandlePacketAsync(RequestType, payload, session, ct))
            return;

        await directMapLinkTransitionService.OpenPendingAreaMapSelectionAsync(session, request.IslandId, ct);
    }
}
