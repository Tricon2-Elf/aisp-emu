using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaEventAreaMapSelectExecRHandler(
    DirectMapLinkTransitionService directMapLinkTransitionService,
    ILogger<AreaEventAreaMapSelectExecRHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.EventAreaMapSelectExecRRequest;
    public PacketType ResponseType => PacketType.EventAreaMapSelectCloseNotify;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = EventAreaMapSelectExecRRequest.FromBytes(payload.Span);
        logger.LogInformation(
            "EventAreaMapSelectExecRRequest from user {UserId}: result {Result}, map {MapId}, channel {ChannelId}",
            session.User?.Id ?? session.UserId,
            request.Result,
            request.MapId,
            request.ChannelId
        );

        await directMapLinkTransitionService.HandleAreaMapSelectionReplyAsync(request, session, ct);
    }
}
