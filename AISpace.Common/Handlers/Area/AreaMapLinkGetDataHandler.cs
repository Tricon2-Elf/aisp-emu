using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaMapLinkGetDataHandler(IMapLinkRepository mapLinkRepository, ILogger<AreaMapLinkGetDataHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.MapLinkGetDataRequest;

    public PacketType ResponseType => PacketType.MapLinkGetDataResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = MapLinkGetDataRequest.FromBytes(payload.Span);
        session.MapId = request.MapId;
        session.ChannelId = (int)request.ChannelId;

        logger.LogInformation("MapLinkGetDataRequest received from user {UserId} on map {MapId} with channel {ChannelId}", session.User?.Id ?? session.UserId, request.MapId, request.ChannelId);
        var response = new MapLinkGetDataResponse(0);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);

        var links = await mapLinkRepository.GetBySourceMapAsync(request.MapId, request.ChannelId, ct);
        var destinationMapIds = new List<uint>(links.Count);

        foreach (var link in links)
        {
            var destinations = link.ParseDestinationMapIds();
            if (destinations.Count != 1)
            {
                logger.LogWarning("Skipping MapLink {MapLinkId} on map {MapId}: direct maplink flow requires exactly one destination, found {DestinationCount}", link.Id, request.MapId, destinations.Count);
                continue;
            }

            destinationMapIds.Add(destinations[0]);

            var mapLinkData = new MapLinkData(link.PositionX, link.PositionY, link.PositionZ, link.Yaw, link.Length, link.Depth);
            await session.SendAsync(PacketType.MapLinkNotifyData, new MapLinkNotifyData(0, mapLinkData).ToBytes(), ct);
        }

        if (destinationMapIds.Count > 0)
            await session.SendAsync(PacketType.NotifySelectMap, new NotifySelectMapData(destinationMapIds).ToBytes(), ct);
    }
}
