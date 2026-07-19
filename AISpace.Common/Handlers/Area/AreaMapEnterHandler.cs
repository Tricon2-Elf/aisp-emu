using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Game.ServerScripts;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaMapEnterHandler(IMapRepository mapRepository, DirectMapLinkTransitionService directMapLinkTransitionService, ILogger<AreaMapEnterHandler> logger, ServerScriptDispatcher? serverScriptDispatcher = null) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MapEnterRequest;
    public PacketType ResponseType => PacketType.MapEnterResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = AreaMapEnterRequest.FromBytes(payload.Span);
        logger.LogInformation("MapEnterRequest from user {UserId}: requested MapID {MapId}, ChannelID {ChannelId}", session.User?.Id ?? session.UserId, request.MapID, request.ChannelId);

        if (session.IsMapTransitionPending)
        {
            if (request.MapID != session.MapId || request.ChannelId != (uint)session.ChannelId)
            {
                logger.LogWarning("Rejecting MapEnterRequest for user {UserId} during pending transition: requested map {RequestedMapId}, channel {RequestedChannelId}, but session is on map {SessionMapId}, channel {SessionChannelId}", session.User?.Id ?? session.UserId, request.MapID, request.ChannelId, session.MapId, session.ChannelId);
                await session.SendAsync(ResponseType, new AreaMapEnterResponse(1).ToBytes(), ct);
                return;
            }

            logger.LogInformation("Acknowledging post-NotifyChangeMap MapEnterRequest for user {UserId} on map {MapId}, channel {ChannelId}", session.User?.Id ?? session.UserId, request.MapID, request.ChannelId);
            await session.SendAsync(ResponseType, new AreaMapEnterResponse(0).ToBytes(), ct);
            await TryNotifyServerScriptsAsync(payload, session, ct);
            return;
        }

        var character = await directMapLinkTransitionService.ResolveCharacterAsync(session, ct);
        if (character == null)
        {
            logger.LogWarning("Rejecting MapEnterRequest for user {UserId}: character could not be resolved", session.User?.Id ?? session.UserId);
            await session.SendAsync(ResponseType, new AreaMapEnterResponse(1).ToBytes(), ct);
            return;
        }

        if (request.MapID == session.MapId && request.ChannelId == (uint)session.ChannelId)
        {
            if (!session.HasMovedSinceMapLoad)
            {
                logger.LogInformation("Treating MapEnterRequest for current map {MapId} and channel {ChannelId} as a post-load acknowledgement for user {UserId}; no movement has been observed on this map yet", request.MapID, request.ChannelId, session.User?.Id ?? session.UserId);
                await session.SendAsync(ResponseType, new AreaMapEnterResponse(0).ToBytes(), ct);
                await TryNotifyServerScriptsAsync(payload, session, ct);
                return;
            }

            if (await directMapLinkTransitionService.TryHandleMapEnterTriggerAsync(request, session, ct))
                return;

            logger.LogInformation("No direct MapLink matched current-map MapEnterRequest for user {UserId} on map {MapId}, channel {ChannelId}, position ({X}, {Y}, {Z}); treating as no-op acknowledgement", session.User?.Id ?? session.UserId, request.MapID, request.ChannelId, session.X, session.Y, session.Z);
            await session.SendAsync(ResponseType, new AreaMapEnterResponse(0).ToBytes(), ct);
            await TryNotifyServerScriptsAsync(payload, session, ct);
            return;
        }

        var map = await mapRepository.GetByMapIdAsync(request.MapID, ct);
        if (map == null)
        {
            logger.LogWarning("Rejecting MapEnterRequest for unknown MapID {MapId}", request.MapID);
            await session.SendAsync(ResponseType, new AreaMapEnterResponse(1).ToBytes(), ct);
            return;
        }

        await directMapLinkTransitionService.CompleteMapTransitionAsync(session, character, request.MapID, request.ChannelId, map, notifyChangeMap: null, sendMapEnterResponse: true, ct);
        await TryNotifyServerScriptsAsync(payload, session, ct);
    }

    private async Task TryNotifyServerScriptsAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct)
    {
        if (serverScriptDispatcher is not null)
            await serverScriptDispatcher.TryHandlePacketAsync(RequestType, payload, session, ct);
    }
}
