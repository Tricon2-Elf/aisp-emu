using AISpace.Common.Config;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AISpace.Common.Handlers.Area;

public sealed class DirectMapLinkTransitionService(IMapRepository mapRepository, ICharacterRepository characterRepository, IMapLinkRepository mapLinkRepository, IChannelRepository channelRepository, IOptions<ServerOptions> serverOptions, SharedState state, ILogger<DirectMapLinkTransitionService> logger)
{
    private const float HitRadius = 125f;
    private const float FallbackRadius = 500f;

    public async Task<DAL.Entities.Character?> ResolveCharacterAsync(IPlayerSession session, CancellationToken ct = default)
    {
        if (session.Character != null)
            return session.Character;

        if (session.CharacterId != 0)
            return await characterRepository.GetByIdAsync((int)session.CharacterId, ct);

        var fallback = session.User?.Characters.FirstOrDefault();
        if (fallback == null)
            return null;

        return await characterRepository.GetByIdAsync(fallback.Id, ct) ?? fallback;
    }

    public async Task<bool> TryHandleMapEnterTriggerAsync(AreaMapEnterRequest request, IPlayerSession session, CancellationToken ct = default)
    {
        if (session.IsMapTransitionPending)
            return false;

        var character = await ResolveCharacterAsync(session, ct);
        if (character == null)
            return false;

        var resolvedLink = await ResolveTriggeredDirectMapLinkAsync(request.MapID, request.ChannelId, [new PositionSample(session.X, session.Z)], allowSingleLinkFallback: true, ct);

        if (resolvedLink == null)
            return false;

        var areaServerInfo = await ResolveAreaServerInfoAsync((int)request.ChannelId, ct);
        var notifyChangeMap = CreateNotifyChangeMap(request.ChannelId, resolvedLink.Value.DestinationMapId, resolvedLink.Value.DestinationMap, areaServerInfo);

        logger.LogInformation(
            "Resolved MapEnterRequest trigger on map {SourceMapId} at position ({X}, {Y}, {Z}) to MapLink {MapLinkId} -> destination map {DestinationMapId} via {Ip}:{Port} (channel {ChannelId}, flag {Flag}, fade {FadeFlag}){FallbackSuffix}",
            request.MapID,
            session.X,
            session.Y,
            session.Z,
            resolvedLink.Value.Link.Id,
            resolvedLink.Value.DestinationMapId,
            areaServerInfo.IP,
            areaServerInfo.Port,
            request.ChannelId,
            notifyChangeMap.Flag,
            notifyChangeMap.FadeFlag,
            resolvedLink.Value.UsedFallback ? " using fallback resolution" : string.Empty
        );

        await CompleteMapTransitionAsync(session, character, resolvedLink.Value.DestinationMapId, request.ChannelId, resolvedLink.Value.DestinationMap, notifyChangeMap, sendMapEnterResponse: true, ct);

        return true;
    }

    public async Task<bool> TryHandleMovementTriggerAsync(IPlayerSession session, IReadOnlyList<PositionSample> samples, CancellationToken ct = default)
    {
        if (session.IsMapTransitionPending || session.MapId == 0 || session.ChannelId == 0 || samples.Count == 0)
            return false;

        var character = await ResolveCharacterAsync(session, ct);
        if (character == null)
            return false;

        var resolvedLink = await ResolveTriggeredDirectMapLinkAsync(session.MapId, (uint)session.ChannelId, samples, allowSingleLinkFallback: false, ct);

        if (resolvedLink == null)
            return false;

        var areaServerInfo = await ResolveAreaServerInfoAsync(session.ChannelId, ct);
        var notifyChangeMap = CreateNotifyChangeMap((uint)session.ChannelId, resolvedLink.Value.DestinationMapId, resolvedLink.Value.DestinationMap, areaServerInfo);

        logger.LogInformation(
            "Resolved movement-based MapLink trigger on map {SourceMapId} at position ({X}, {Y}, {Z}) to MapLink {MapLinkId} -> destination map {DestinationMapId} via {Ip}:{Port} (channel {ChannelId}, flag {Flag}, fade {FadeFlag}) using {SampleCount} movement samples{FallbackSuffix}",
            session.MapId,
            session.X,
            session.Y,
            session.Z,
            resolvedLink.Value.Link.Id,
            resolvedLink.Value.DestinationMapId,
            areaServerInfo.IP,
            areaServerInfo.Port,
            session.ChannelId,
            notifyChangeMap.Flag,
            notifyChangeMap.FadeFlag,
            samples.Count,
            resolvedLink.Value.UsedFallback ? " using fallback resolution" : string.Empty
        );

        await CompleteMapTransitionAsync(session, character, resolvedLink.Value.DestinationMapId, (uint)session.ChannelId, resolvedLink.Value.DestinationMap, notifyChangeMap, sendMapEnterResponse: true, ct);

        return true;
    }

    public async Task CompleteMapTransitionAsync(IPlayerSession session, DAL.Entities.Character character, uint destinationMapId, uint destinationChannelId, DAL.Entities.Map destinationMap, NotifyChangeMap? notifyChangeMap, bool sendMapEnterResponse, CancellationToken ct = default)
    {
        if (notifyChangeMap != null)
            session.IsMapTransitionPending = true;

        var oldPeers = state.GetAreaPeers(session).ToList();
        var disappearPacket = new NotifyDisappearChara(session.CharacterId).ToBytes();
        foreach (var other in oldPeers)
        {
            await other.SendAsync(PacketType.NotifyDisappearChara, disappearPacket, ct);
        }

        var updatedCharacter = await characterRepository.UpdateCurrentMapAsync(character.Id, destinationMapId, ct) ?? character;
        updatedCharacter.CurrentMapId = destinationMapId;

        session.Character = updatedCharacter;
        session.CharacterId = (uint)updatedCharacter.Id;
        session.MapId = destinationMapId;
        session.ChannelId = (int)destinationChannelId;
        session.X = destinationMap.SpawnX;
        session.Y = destinationMap.SpawnY;
        session.Z = destinationMap.SpawnZ;
        session.Rotation = (sbyte)destinationMap.SpawnRotation;
        session.MovementTypeId = (int)MovementType.Stopped;
        session.HasMovedSinceMapLoad = false;
        session.IsMapTransitionPending = notifyChangeMap != null;

        var userCharacter = session.User?.Characters.FirstOrDefault(candidate => candidate.Id == updatedCharacter.Id);
        if (userCharacter != null)
            userCharacter.CurrentMapId = destinationMapId;

        if (notifyChangeMap != null && session.User != null)
        {
            state.SetPendingAreaTransition(new SharedState.PendingAreaTransition(session.User.Id, destinationMapId, (int)destinationChannelId, destinationMap.SpawnX, destinationMap.SpawnY, destinationMap.SpawnZ, (sbyte)destinationMap.SpawnRotation));
        }

        if (sendMapEnterResponse)
            await session.SendAsync(PacketType.MapEnterResponse, new AreaMapEnterResponse(0).ToBytes(), ct);

        if (notifyChangeMap != null)
            await session.SendAsync(PacketType.NotifyChangeMap, notifyChangeMap.ToBytes(), ct);
    }

    private async Task<ServerInfo> ResolveAreaServerInfoAsync(int channelId, CancellationToken ct)
    {
        var currentChannel = await channelRepository.GetByChannelNumAsync(channelId, ct);
        if (currentChannel == null)
        {
            logger.LogWarning("Channel {ChannelId} was not found while building NotifyChangeMap; falling back to localhost:50054", channelId);
            return new ServerInfo(serverOptions.Value.ResolveAddress("localhost"), 50054);
        }

        return new ServerInfo(serverOptions.Value.ResolveAddress(currentChannel.IP), currentChannel.Port);
    }

    private NotifyChangeMap CreateNotifyChangeMap(uint channelId, uint destinationMapId, DAL.Entities.Map destinationMap, ServerInfo areaServerInfo)
    {
        return new NotifyChangeMap
        {
            ChannelId = channelId,
            MapId = destinationMapId,
            MapSerialId = destinationMapId,
            RouteState = 0,
            PositionX = destinationMap.SpawnX,
            PositionY = destinationMap.SpawnY,
            PositionZ = destinationMap.SpawnZ,
            Rotation = (sbyte)destinationMap.SpawnRotation,
            Animation = (byte)MovementType.Stopped,
            // Decompiled transition handling checks bit 0x2 on both flag bytes.
            Flag = 0,
            AreaServerInfo = areaServerInfo,
            FadeFlag = 0,
        };
    }

    private async Task<ResolvedMapLink?> ResolveTriggeredDirectMapLinkAsync(uint sourceMapId, uint channelId, IReadOnlyList<PositionSample> samples, bool allowSingleLinkFallback, CancellationToken ct)
    {
        var links = await mapLinkRepository.GetBySourceMapAsync(sourceMapId, channelId, ct);
        var directRoutes = new List<ResolvedMapLink>();
        ResolvedMapLink? insideMatch = null;
        ResolvedMapLink? nearbyMatch = null;

        foreach (var link in links)
        {
            var destinations = link.ParseDestinationMapIds();
            if (destinations.Count != 1)
                continue;

            var destinationMapId = destinations[0];
            var destinationMap = await mapRepository.GetByMapIdAsync(destinationMapId, ct);
            if (destinationMap == null)
            {
                logger.LogWarning("Skipping direct MapLink {MapLinkId} on map {SourceMapId}: destination map {DestinationMapId} was not found", link.Id, sourceMapId, destinationMapId);
                continue;
            }

            var route = new ResolvedMapLink(link, destinationMapId, destinationMap, UsedFallback: false, DistanceSquared: 0f);
            directRoutes.Add(route);

            var match = ScoreMapLink(link, samples);
            if (match.IsInside)
            {
                if (insideMatch == null || match.DistanceSquared < insideMatch.Value.DistanceSquared)
                    insideMatch = route with { DistanceSquared = match.DistanceSquared };

                continue;
            }

            if (match.IsNear)
            {
                if (nearbyMatch == null || match.DistanceSquared < nearbyMatch.Value.DistanceSquared)
                    nearbyMatch = route with { UsedFallback = true, DistanceSquared = match.DistanceSquared };
            }
        }

        if (insideMatch != null)
            return insideMatch;

        if (nearbyMatch != null)
            return nearbyMatch;

        if (allowSingleLinkFallback && directRoutes.Count == 1)
            return directRoutes[0] with { UsedFallback = true };

        return null;
    }

    private static MapLinkMatch ScoreMapLink(DAL.Entities.MapLink link, IReadOnlyList<PositionSample> samples)
    {
        var bestDistanceSquared = float.MaxValue;
        var inside = false;
        var near = false;

        foreach (var sample in samples)
        {
            var distanceSquared = MapLinkGeometry.DistanceSquaredToLane(link, sample.X, sample.Z);
            if (distanceSquared < bestDistanceSquared)
                bestDistanceSquared = distanceSquared;

            if (distanceSquared <= HitRadius * HitRadius)
                inside = true;

            if (distanceSquared <= FallbackRadius * FallbackRadius)
                near = true;
        }

        return new MapLinkMatch(inside, near, bestDistanceSquared);
    }

    public readonly record struct PositionSample(float X, float Z);

    private readonly record struct ResolvedMapLink(DAL.Entities.MapLink Link, uint DestinationMapId, DAL.Entities.Map DestinationMap, bool UsedFallback, float DistanceSquared);

    private readonly record struct MapLinkMatch(bool IsInside, bool IsNear, float DistanceSquared);
}
