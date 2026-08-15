using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

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
