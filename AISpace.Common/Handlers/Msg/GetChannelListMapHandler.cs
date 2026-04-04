using AISpace.Common.Config;
using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Handlers.Area;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using AISpace.Network.Packets.Msg;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AISpace.Common.Handlers.Msg;

public class GetChannelListMapHandler(IOptions<ServerOptions> serverOptions, IChannelRepository channelRepo, SharedState state, DirectMapLinkTransitionService directMapLinkTransitionService, ILogger<GetChannelListMapHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.GetChannelListMapRequest;
    public PacketType ResponseType => PacketType.GetChannelListMapResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = GetChannelListMapRequest.FromBytes(payload.Span);
        var dbChannels = await channelRepo.GetAllAsync(ct);

        var matchingChannels = dbChannels.Where(channel => channel.MapId == request.MapId).OrderBy(channel => channel.ChannelNum).ToList();

        if (matchingChannels.Count == 0)
        {
            var requestedGroup = request.MapId / 10_000u;
            matchingChannels = dbChannels.Where(channel => channel.MapId / 10_000u == requestedGroup).OrderBy(channel => channel.ChannelNum).ToList();

            if (matchingChannels.Count > 1 && session.ChannelId != 0)
            {
                var currentChannelMatches = matchingChannels.Where(channel => channel.ChannelNum == session.ChannelId).ToList();

                if (currentChannelMatches.Count != 0)
                {
                    logger.LogInformation("GetChannelListMapRequest for map {MapId} narrowed fallback channel list from {OriginalCount} to current channel {ChannelId}", request.MapId, matchingChannels.Count, session.ChannelId);
                    matchingChannels = currentChannelMatches;
                }
            }

            if (matchingChannels.Count > 0)
            {
                logger.LogInformation("GetChannelListMapRequest for map {MapId} matched {Count} channel(s) by map group fallback {MapGroup}", request.MapId, matchingChannels.Count, requestedGroup);
            }
        }

        logger.LogInformation("GetChannelListMapRequest from user {UserId}: map {MapId}, returning {Count} channel(s)", session.User?.Id ?? session.UserId, request.MapId, matchingChannels.Count);

        var channels = matchingChannels.Select(channel => ToChannelInfo(channel, serverOptions.Value)).ToList();

        await session.SendAsync(ResponseType, new GetChannelListMapResponse(0, channels).ToBytes(), ct);
        await TryCompletePendingAreaMapSelectionAsync(request, session, matchingChannels, ct);
    }

    private static ChannelInfo ToChannelInfo(GameChannel channel, ServerOptions serverOptions)
    {
        var maxUsers = channel.MaxUsers != 0 ? channel.MaxUsers : 1000u;
        var currentUsers = channel.CurrentUsers > maxUsers ? maxUsers : channel.CurrentUsers;
        return new ChannelInfo((uint)channel.ChannelNum, currentUsers, maxUsers, new ServerInfo(serverOptions.ResolveAddress(channel.IP), channel.Port));
    }

    private async Task TryCompletePendingAreaMapSelectionAsync(GetChannelListMapRequest request, IPlayerSession session, IReadOnlyList<GameChannel> matchingChannels, CancellationToken ct)
    {
        if (matchingChannels.Count != 1)
            return;

        var userId = session.User?.Id ?? session.UserId;
        if (userId == 0)
            return;

        var areaSession = state.GetAreaSessionByUserId(userId);
        var pendingSelection = areaSession?.PendingAreaMapSelection;
        if (areaSession == null || pendingSelection == null)
            return;

        var channelId = (uint)matchingChannels[0].ChannelNum;
        if (!pendingSelection.Destinations.Any(destination => destination.MapId == request.MapId && destination.ChannelId == channelId))
            return;

        logger.LogInformation("Applying selector auto-selection compatibility path for user {UserId}: map {MapId}, channel {ChannelId}", userId, request.MapId, channelId);

        await directMapLinkTransitionService.HandleAreaMapSelectionReplyAsync(
            new EventAreaMapSelectExecRRequest
            {
                Result = 0,
                MapId = request.MapId,
                ChannelId = channelId,
            },
            areaSession,
            ct
        );
    }
}
