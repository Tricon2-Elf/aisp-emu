using AISpace.Common.Config;
using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Msg;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AISpace.Common.Handlers.Msg;

public class GetChannelListMapHandler(IOptions<ServerOptions> serverOptions, IChannelRepository channelRepo, ILogger<GetChannelListMapHandler> logger) : IPacketHandler
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
    }

    private static ChannelInfo ToChannelInfo(GameChannel channel, ServerOptions serverOptions)
    {
        return new ChannelInfo((uint)channel.ChannelNum, channel.CurrentUsers, channel.MaxUsers, new ServerInfo(serverOptions.ResolveAddress(channel.IP), channel.Port));
    }
}
