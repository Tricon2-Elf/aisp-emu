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

public class GetChannelListMapHandler(
    IOptions<ServerOptions> serverOptions,
    IChannelRepository channelRepo,
    SharedState state,
    DirectMapLinkTransitionService directMapLinkTransitionService,
    ILogger<GetChannelListMapHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.GetChannelListMapRequest;
    public PacketType ResponseType => PacketType.GetChannelListMapResponse;
    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = GetChannelListMapRequest.FromBytes(payload.Span);
        var dbChannels = await channelRepo.GetAllAsync(ct);

        // Always advertise the player's current channel. Destination maps (e.g. My Room) often have no
        // channel rows of their own; returning an empty list leaves the client stuck in the selector.
        var matchingChannels =
            session.ChannelId == 0
                ? []
                : dbChannels
                    .Where(channel => channel.ChannelNum == session.ChannelId)
                    .OrderBy(channel => channel.ChannelNum)
                    .ToList();

        logger.LogInformation(
            "GetChannelListMapRequest from user {UserId}: map {MapId}, returning {Count} channel(s) (current channel {ChannelId})",
            session.User?.Id ?? session.UserId,
            request.MapId,
            matchingChannels.Count,
            session.ChannelId
        );

        var channels = matchingChannels
            .Select(channel => ToChannelInfo(channel, serverOptions.Value))
            .ToList();

        await session.SendAsync(
            ResponseType,
            new GetChannelListMapResponse(0, channels).ToBytes(),
            ct
        );
        await TryCompletePendingAreaMapSelectionAsync(request, session, matchingChannels, ct);
    }

    private static ChannelInfo ToChannelInfo(GameChannel channel, ServerOptions serverOptions)
    {
        var maxUsers = channel.MaxUsers != 0 ? channel.MaxUsers : 1000u;
        var currentUsers = channel.CurrentUsers > maxUsers ? maxUsers : channel.CurrentUsers;
        return new ChannelInfo(
            (uint)channel.ChannelNum,
            currentUsers,
            maxUsers,
            new ServerInfo(serverOptions.ResolveAddress(channel.IP), channel.Port)
        );
    }

    private async Task TryCompletePendingAreaMapSelectionAsync(
        GetChannelListMapRequest request,
        IPlayerSession session,
        IReadOnlyList<GameChannel> matchingChannels,
        CancellationToken ct
    )
    {
        if (matchingChannels.Count != 1)
            return;

        var userId = session.User?.Id ?? session.UserId;
        if (userId == 0)
            return;

        var areaSession = state.GetAreaSessionByUserId(userId);
        var pendingSelection = areaSession?.PendingAreaMapSelection;
        if (areaSession == null || pendingSelection == null)
        {
            if (areaSession == null)
                logger.LogDebug(
                    "GetChannelListMapHandler: No area session found for user {UserId} (area server may be in separate process)",
                    userId
                );
            return;
        }

        var channelId = (uint)matchingChannels[0].ChannelNum;
        if (
            !pendingSelection.Destinations.Any(destination =>
                destination.MapId == request.MapId && destination.ChannelId == channelId
            )
        )
            return;

        logger.LogInformation(
            "Applying selector auto-selection compatibility path for user {UserId}: map {MapId}, channel {ChannelId}",
            userId,
            request.MapId,
            channelId
        );

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
