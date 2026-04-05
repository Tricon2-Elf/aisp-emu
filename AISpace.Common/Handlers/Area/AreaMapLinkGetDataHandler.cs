using AISpace.Common.Config;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AISpace.Common.Handlers.Area;

public class AreaMapLinkGetDataHandler(IMapLinkRepository mapLinkRepository, IMapRepository mapRepository, IChannelRepository channelRepository, IOptions<ServerOptions> serverOptions, ILogger<AreaMapLinkGetDataHandler> logger) : IPacketHandler
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
        var currentChannel = await channelRepository.GetByChannelNumAsync((int)request.ChannelId, ct);
        var areaServerInfo = currentChannel is null ? new ServerInfo(serverOptions.Value.ResolveAddress("localhost"), 50054) : new ServerInfo(serverOptions.Value.ResolveAddress(currentChannel.IP), currentChannel.Port);
        var selectEntries = new List<NotifySelectMapEntry>(links.Count);

        if (currentChannel is null)
        {
            logger.LogWarning("Channel {ChannelId} was not found while building NotifySelectMap for map {MapId}; falling back to {Ip}:{Port}", request.ChannelId, request.MapId, areaServerInfo.IP, areaServerInfo.Port);
        }

        foreach (var link in links)
        {
            var destinations = link.ParseDestinationMapIds();
            if (destinations.Count == 0)
            {
                logger.LogWarning("Skipping MapLink {MapLinkId} on map {MapId}: no valid destinations were configured", link.Id, request.MapId);
                continue;
            }

            var lane = MapLinkGeometry.GetTriggerLane(link);
            var mapLinkData = new MapLinkData(link.PositionX, link.PositionY, link.PositionZ, link.Yaw, link.Length, link.Depth);
            await session.SendAsync(PacketType.MapLinkNotifyData, new MapLinkNotifyData(0, mapLinkData).ToBytes(), ct);

            if (link.Behavior == DAL.Entities.MapLinkBehavior.ForceSelection || destinations.Count != 1)
            {
                logger.LogInformation("Sending selector MapLink {MapLinkId} on map {SourceMapId} with {DestinationCount} destination(s); trigger lane ({StartX}, {StartZ}) -> ({EndX}, {EndZ})", link.Id, request.MapId, destinations.Count, lane.StartX, lane.StartZ, lane.EndX, lane.EndZ);
                continue;
            }

            var destinationMapId = destinations[0];
            var destinationMap = await mapRepository.GetByMapIdAsync(destinationMapId, ct);
            if (destinationMap is null)
            {
                logger.LogWarning("Destination map {DestinationMapId} for MapLink {MapLinkId} on map {SourceMapId} was not found; falling back to zeroed route data", destinationMapId, link.Id, request.MapId);
            }

            selectEntries.Add(
                new NotifySelectMapEntry
                {
                    MapId = destinationMapId,
                    AreaServerInfo = areaServerInfo,
                    ChannelId = request.ChannelId,
                    RouteMapId = destinationMapId,
                    MapSerialId = destinationMapId,
                    RouteState = 0,
                    PositionX = destinationMap?.SpawnX ?? 0f,
                    PositionY = destinationMap?.SpawnY ?? 0f,
                    PositionZ = destinationMap?.SpawnZ ?? 0f,
                    Yaw = (byte)(destinationMap?.SpawnRotation ?? 0),
                    Animation = 0,
                    Unknown1 = 0,
                    Unknown2 = 0,
                }
            );

            logger.LogInformation(
                "Sending direct MapLink {MapLinkId} on map {SourceMapId} to destination map {DestinationMapId} via {Ip}:{Port} (channel {ChannelId}) at spawn ({SpawnX}, {SpawnY}, {SpawnZ}) yaw {Yaw}; trigger lane ({StartX}, {StartZ}) -> ({EndX}, {EndZ})",
                link.Id,
                request.MapId,
                destinationMapId,
                areaServerInfo.IP,
                areaServerInfo.Port,
                request.ChannelId,
                destinationMap?.SpawnX ?? 0f,
                destinationMap?.SpawnY ?? 0f,
                destinationMap?.SpawnZ ?? 0f,
                destinationMap?.SpawnRotation ?? 0,
                lane.StartX,
                lane.StartZ,
                lane.EndX,
                lane.EndZ
            );
        }

        if (selectEntries.Count > 0)
            await session.SendAsync(PacketType.NotifySelectMap, new NotifySelectMapData(selectEntries).ToBytes(), ct);
    }
}
