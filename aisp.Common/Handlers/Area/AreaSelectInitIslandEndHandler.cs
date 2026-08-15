using aisp.Common.Game;
using aisp.Common.Game.ServerScripts;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public sealed class AreaSelectInitIslandEndHandler(
    DirectMapLinkTransitionService directMapLinkTransitionService,
    ServerScriptDispatcher serverScriptDispatcher,
    ILogger<AreaSelectInitIslandEndHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.SelectInitIslandEndRequest;
    public PacketType ResponseType => PacketType.SelectInitIslandStart;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = SelectInitIslandEndRequest.FromBytes(payload.Span);
        logger.LogInformation(
            "SelectInitIslandEndRequest from user {UserId}: island {IslandId}",
            session.User?.Id ?? session.UserId,
            request.IslandId
        );

        if (await serverScriptDispatcher.TryHandlePacketAsync(RequestType, payload, session, ct))
            return;

        await directMapLinkTransitionService.OpenPendingAreaMapSelectionAsync(
            session,
            request.IslandId,
            ct
        );
    }
}
