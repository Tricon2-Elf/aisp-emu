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
    private const uint SelectorSuccess = 0;
    private const uint SelectorFailure = 1;

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

        var resolvedLink = await ResolveTriggeredMapLinkAsync(request.MapID, request.ChannelId, [new PositionSample(session.X, session.Z)], allowSingleLinkFallback: true, ct);

        if (resolvedLink == null)
            return false;

        return await ExecuteTriggeredMapLinkAsync("MapEnterRequest", request.MapID, request.ChannelId, session, character, resolvedLink.Value, sendMapEnterResponseForDirect: true, sendMapEnterResponseForSelection: true, samplesCount: 1, ct);
    }

    public async Task<bool> TryHandleMovementTriggerAsync(IPlayerSession session, IReadOnlyList<PositionSample> samples, CancellationToken ct = default)
    {
        if (session.IsMapTransitionPending || session.PendingAreaMapSelection != null || session.MapId == 0 || session.ChannelId == 0 || samples.Count == 0)
            return false;

        var character = await ResolveCharacterAsync(session, ct);
        if (character == null)
            return false;

        var resolvedLink = await ResolveTriggeredMapLinkAsync(session.MapId, (uint)session.ChannelId, samples, allowSingleLinkFallback: false, ct);

        if (resolvedLink == null)
            return false;

        return await ExecuteTriggeredMapLinkAsync("movement-based", session.MapId, (uint)session.ChannelId, session, character, resolvedLink.Value, sendMapEnterResponseForDirect: true, sendMapEnterResponseForSelection: false, samplesCount: samples.Count, ct);
    }

    public async Task<bool> HandleAreaMapSelectionReplyAsync(EventAreaMapSelectExecRRequest request, IPlayerSession session, CancellationToken ct = default)
    {
        if (session.PendingAreaMapSelection == null)
        {
            if (session.IsMapTransitionPending)
            {
                logger.LogInformation("Ignoring area-map selection reply from user {UserId}: a map transition is already pending on map {MapId}, channel {ChannelId}", session.User?.Id ?? session.UserId, session.MapId, session.ChannelId);
                return true;
            }

            logger.LogWarning("Rejecting area-map selection reply from user {UserId}: no selector is pending on map {MapId}, channel {ChannelId}", session.User?.Id ?? session.UserId, session.MapId, session.ChannelId);
            await session.SendAsync(PacketType.EventAreaMapSelectCloseNotify, new EventAreaMapSelectCloseNotify(SelectorFailure).ToBytes(), ct);
            return true;
        }

        var selection = session.PendingAreaMapSelection;
        session.PendingAreaMapSelection = null;

        if (request.Result != SelectorSuccess)
        {
            logger.LogInformation("Closing area-map selector for user {UserId} with client result {Result}", session.User?.Id ?? session.UserId, request.Result);
            await session.SendAsync(PacketType.EventAreaMapSelectCloseNotify, new EventAreaMapSelectCloseNotify(request.Result).ToBytes(), ct);
            return true;
        }

        var selectedDestination = selection.Destinations.FirstOrDefault(destination => destination.MapId == request.MapId && destination.ChannelId == request.ChannelId);
        if (selectedDestination == null)
        {
            logger.LogWarning("Rejecting area-map selection reply from user {UserId}: map {MapId}, channel {ChannelId} is not one of the offered destinations for MapLink {MapLinkId}", session.User?.Id ?? session.UserId, request.MapId, request.ChannelId, selection.LinkId);
            await session.SendAsync(PacketType.EventAreaMapSelectCloseNotify, new EventAreaMapSelectCloseNotify(SelectorFailure).ToBytes(), ct);
            return true;
        }

        var character = await ResolveCharacterAsync(session, ct);
        if (character == null)
        {
            logger.LogWarning("Rejecting area-map selection reply from user {UserId}: character could not be resolved", session.User?.Id ?? session.UserId);
            await session.SendAsync(PacketType.EventAreaMapSelectCloseNotify, new EventAreaMapSelectCloseNotify(SelectorFailure).ToBytes(), ct);
            return true;
        }

        var destinationMap = await mapRepository.GetByMapIdAsync(selectedDestination.MapId, ct);
        if (destinationMap == null)
        {
            logger.LogWarning("Rejecting area-map selection reply from user {UserId}: destination map {MapId} was not found", session.User?.Id ?? session.UserId, selectedDestination.MapId);
            await session.SendAsync(PacketType.EventAreaMapSelectCloseNotify, new EventAreaMapSelectCloseNotify(SelectorFailure).ToBytes(), ct);
            return true;
        }

        await session.SendAsync(PacketType.EventAreaMapSelectCloseNotify, new EventAreaMapSelectCloseNotify(SelectorSuccess).ToBytes(), ct);

        var areaServerInfo = await ResolveAreaServerInfoAsync((int)selectedDestination.ChannelId, ct);
        var notifyChangeMap = CreateNotifyChangeMap(selectedDestination.ChannelId, selectedDestination.MapId, destinationMap, areaServerInfo);

        logger.LogInformation(
            "Accepted area-map selection for user {UserId}: MapLink {MapLinkId} -> destination map {DestinationMapId} via {Ip}:{Port} (channel {ChannelId}, flag {Flag}, fade {FadeFlag})",
            session.User?.Id ?? session.UserId,
            selection.LinkId,
            selectedDestination.MapId,
            areaServerInfo.IP,
            areaServerInfo.Port,
            selectedDestination.ChannelId,
            notifyChangeMap.Flag,
            notifyChangeMap.FadeFlag
        );

        await CompleteMapTransitionAsync(session, character, selectedDestination.MapId, selectedDestination.ChannelId, destinationMap, notifyChangeMap, sendMapEnterResponse: false, ct);
        return true;
    }

    public async Task<bool> OpenPendingAreaMapSelectionAsync(IPlayerSession session, uint selectedIslandId, CancellationToken ct = default)
    {
        var selection = session.PendingAreaMapSelection;
        if (selection == null)
        {
            logger.LogWarning("Ignoring SelectInitIslandEndRequest from user {UserId}: no selector is pending on map {MapId}, channel {ChannelId}", session.User?.Id ?? session.UserId, session.MapId, session.ChannelId);
            return false;
        }

        if (!selection.AwaitingIslandBootstrapAck || selection.SelectorOpened)
        {
            logger.LogInformation("Ignoring duplicate SelectInitIslandEndRequest from user {UserId} for MapLink {MapLinkId}", session.User?.Id ?? session.UserId, selection.LinkId);
            return true;
        }

        var allowedIslandIds = selection.Destinations.Select(destination => ResolveIslandId(destination.MapId)).Append(selection.IslandId).Distinct().ToList();
        var resolvedIslandId = allowedIslandIds.Contains(selectedIslandId) ? selectedIslandId : selection.IslandId;
        if (resolvedIslandId != selectedIslandId)
        {
            logger.LogWarning("SelectInitIslandEndRequest from user {UserId} acknowledged unknown island {RequestedIslandId} for MapLink {MapLinkId}; falling back to island {IslandId}", session.User?.Id ?? session.UserId, selectedIslandId, selection.LinkId, resolvedIslandId);
        }

        if (selection.SelectorOpened)
        {
            selection.AwaitingIslandBootstrapAck = false;
            logger.LogInformation("Acknowledged island bootstrap for user {UserId}: selector MapLink {MapLinkId} was already open on island {IslandId}", session.User?.Id ?? session.UserId, selection.LinkId, resolvedIslandId);
            return true;
        }

        var selectorEntries = await CreateSelectorEntriesAsync(selection, ct);
        if (selectorEntries.Count == 0)
        {
            logger.LogWarning("Aborting selector MapLink {MapLinkId} for user {UserId}: no valid selector entries could be built after island bootstrap", selection.LinkId, session.User?.Id ?? session.UserId);
            session.PendingAreaMapSelection = null;
            return false;
        }

        selection.AwaitingIslandBootstrapAck = false;
        selection.SelectorOpened = true;

        logger.LogInformation("Opening area-map selector for user {UserId}: MapLink {MapLinkId} with {DestinationCount} destination(s) on island {IslandId}", session.User?.Id ?? session.UserId, selection.LinkId, selectorEntries.Count, resolvedIslandId);

        await session.SendAsync(
            PacketType.EventAreaMapSelectExec,
            new EventAreaMapSelectExecNotify
            {
                Entries = selectorEntries,
                IslandId = resolvedIslandId,
                IsRegisteredIsland = selection.IsRegisteredIsland,
            }.ToBytes(),
            ct
        );

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
        session.PendingAreaMapSelection = null;

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

    private NotifySelectMapEntry CreateSelectMapEntry(uint channelId, uint destinationMapId, DAL.Entities.Map destinationMap, ServerInfo areaServerInfo)
    {
        return new NotifySelectMapEntry
        {
            MapId = destinationMapId,
            AreaServerInfo = areaServerInfo,
            ChannelId = channelId,
            RouteMapId = destinationMapId,
            MapSerialId = destinationMapId,
            RouteState = 0,
            PositionX = destinationMap.SpawnX,
            PositionY = destinationMap.SpawnY,
            PositionZ = destinationMap.SpawnZ,
            Yaw = (byte)(sbyte)destinationMap.SpawnRotation,
            Animation = (byte)MovementType.Stopped,
            Unknown1 = 0,
            Unknown2 = 0,
        };
    }

    private async Task<bool> ExecuteTriggeredMapLinkAsync(string triggerName, uint sourceMapId, uint channelId, IPlayerSession session, DAL.Entities.Character character, ResolvedMapLink resolvedLink, bool sendMapEnterResponseForDirect, bool sendMapEnterResponseForSelection, int samplesCount, CancellationToken ct)
    {
        if (resolvedLink.Kind == MapLinkResolutionKind.Direct)
        {
            var areaServerInfo = await ResolveAreaServerInfoAsync((int)channelId, ct);
            var notifyChangeMap = CreateNotifyChangeMap(channelId, resolvedLink.DestinationMapId, resolvedLink.DestinationMap!, areaServerInfo);

            logger.LogInformation(
                "Resolved {TriggerName} MapLink trigger on map {SourceMapId} at position ({X}, {Y}, {Z}) to MapLink {MapLinkId} -> destination map {DestinationMapId} via {Ip}:{Port} (channel {ChannelId}, flag {Flag}, fade {FadeFlag}){SampleSuffix}{FallbackSuffix}",
                triggerName,
                sourceMapId,
                session.X,
                session.Y,
                session.Z,
                resolvedLink.Link.Id,
                resolvedLink.DestinationMapId,
                areaServerInfo.IP,
                areaServerInfo.Port,
                channelId,
                notifyChangeMap.Flag,
                notifyChangeMap.FadeFlag,
                triggerName == "movement-based" ? $" using {samplesCount} movement samples" : string.Empty,
                resolvedLink.UsedFallback ? " using fallback resolution" : string.Empty
            );

            await CompleteMapTransitionAsync(session, character, resolvedLink.DestinationMapId, channelId, resolvedLink.DestinationMap!, notifyChangeMap, sendMapEnterResponseForDirect, ct);
            return true;
        }

        var selection = new PendingAreaMapSelection
        {
            LinkId = resolvedLink.Link.Id,
            SourceMapId = sourceMapId,
            ChannelId = channelId,
            IslandId = ResolveIslandId(sourceMapId),
            IsRegisteredIsland = 0,
            Destinations = resolvedLink.SelectionDestinations,
        };

        if (session.PendingAreaMapSelection is { } existingSelection && existingSelection.LinkId == selection.LinkId && existingSelection.SourceMapId == selection.SourceMapId && existingSelection.ChannelId == selection.ChannelId)
        {
            if (sendMapEnterResponseForSelection)
                await session.SendAsync(PacketType.MapEnterResponse, new AreaMapEnterResponse(0).ToBytes(), ct);

            return true;
        }

        session.PendingAreaMapSelection = selection;
        session.HasMovedSinceMapLoad = true;

        if (sendMapEnterResponseForSelection)
            await session.SendAsync(PacketType.MapEnterResponse, new AreaMapEnterResponse(0).ToBytes(), ct);

        logger.LogInformation(
            "Resolved {TriggerName} MapLink trigger on map {SourceMapId} at position ({X}, {Y}, {Z}) to selector MapLink {MapLinkId} with {DestinationCount} destination(s){SampleSuffix}{FallbackSuffix}",
            triggerName,
            sourceMapId,
            session.X,
            session.Y,
            session.Z,
            resolvedLink.Link.Id,
            selection.Destinations.Count,
            triggerName == "movement-based" ? $" using {samplesCount} movement samples" : string.Empty,
            resolvedLink.UsedFallback ? " using fallback resolution" : string.Empty
        );

        var selectorEntries = await CreateSelectorEntriesAsync(selection, ct);
        if (selectorEntries.Count == 0)
        {
            logger.LogWarning("Aborting selector MapLink {MapLinkId} for user {UserId}: no valid selector entries could be built", selection.LinkId, session.User?.Id ?? session.UserId);
            session.PendingAreaMapSelection = null;
            return false;
        }

        selection.SelectorOpened = true;

        await session.SendAsync(PacketType.SelectInitIslandStart, (await CreateSelectInitIslandStartAsync(selection, ct)).ToBytes(), ct);
        await session.SendAsync(
            PacketType.EventAreaMapSelectExec,
            new EventAreaMapSelectExecNotify
            {
                Entries = selectorEntries,
                IslandId = selection.IslandId,
                IsRegisteredIsland = selection.IsRegisteredIsland,
            }.ToBytes(),
            ct
        );

        return true;
    }

    private async Task<List<NotifySelectMapEntry>> CreateSelectorEntriesAsync(PendingAreaMapSelection selection, CancellationToken ct)
    {
        var selectorEntries = new List<NotifySelectMapEntry>(selection.Destinations.Count);
        foreach (var destination in selection.Destinations)
        {
            var destinationMap = await mapRepository.GetByMapIdAsync(destination.MapId, ct);
            if (destinationMap == null)
            {
                logger.LogWarning("Skipping selector entry for MapLink {MapLinkId}: destination map {DestinationMapId} was not found while building EventAreaMapSelectExec", selection.LinkId, destination.MapId);
                continue;
            }

            var areaServerInfo = await ResolveAreaServerInfoAsync((int)destination.ChannelId, ct);
            selectorEntries.Add(CreateSelectMapEntry(destination.ChannelId, destination.MapId, destinationMap, areaServerInfo));
        }

        return selectorEntries;
    }

    private async Task<SelectInitIslandStartNotify> CreateSelectInitIslandStartAsync(PendingAreaMapSelection selection, CancellationToken ct)
    {
        var islandIds = selection.Destinations.Select(destination => ResolveIslandId(destination.MapId)).Append(selection.IslandId).Distinct().OrderBy(islandId => islandId).ToList();

        var islands = new List<SelectInitIslandEntry>(islandIds.Count);
        foreach (var islandId in islandIds)
        {
            var destinationMaps = new List<DAL.Entities.Map>();
            foreach (var destination in selection.Destinations.Where(destination => ResolveIslandId(destination.MapId) == islandId))
            {
                var destinationMap = await mapRepository.GetByMapIdAsync(destination.MapId, ct);
                if (destinationMap != null)
                    destinationMaps.Add(destinationMap);
            }

            var title = ResolveIslandTitle(islandId, destinationMaps);
            var descriptionLines = destinationMaps.Select(destinationMap => destinationMap.Name).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().ToList();

            if (descriptionLines.Count == 0)
                descriptionLines.Add(title);

            islands.Add(
                new SelectInitIslandEntry
                {
                    IslandId = islandId,
                    Title = title,
                    Description = string.Join("\n", descriptionLines),
                }
            );
        }

        return new SelectInitIslandStartNotify { Islands = islands };
    }

    private async Task<ResolvedMapLink?> ResolveTriggeredMapLinkAsync(uint sourceMapId, uint channelId, IReadOnlyList<PositionSample> samples, bool allowSingleLinkFallback, CancellationToken ct)
    {
        var links = await mapLinkRepository.GetBySourceMapAsync(sourceMapId, channelId, ct);
        var candidates = new List<ResolvedMapLink>();
        ResolvedMapLink? insideMatch = null;
        ResolvedMapLink? nearbyMatch = null;

        foreach (var link in links)
        {
            var destinations = link.ParseDestinationMapIds();
            if (destinations.Count == 0)
                continue;

            ResolvedMapLink? route;
            if (RequiresSelection(link, destinations))
            {
                var selectionDestinations = new List<AreaMapSelectionDestination>(destinations.Count);
                foreach (var destinationMapId in destinations)
                {
                    var destinationMap = await mapRepository.GetByMapIdAsync(destinationMapId, ct);
                    if (destinationMap == null)
                    {
                        logger.LogWarning("Skipping selector destination map {DestinationMapId} for MapLink {MapLinkId} on map {SourceMapId}: destination map was not found", destinationMapId, link.Id, sourceMapId);
                        continue;
                    }

                    selectionDestinations.Add(new AreaMapSelectionDestination(destinationMapId, channelId));
                }

                if (selectionDestinations.Count == 0)
                    continue;

                if (selectionDestinations.Count == 1)
                {
                    var fallbackDestination = selectionDestinations[0];
                    var fallbackMap = await mapRepository.GetByMapIdAsync(fallbackDestination.MapId, ct);
                    if (fallbackMap == null)
                        continue;

                    logger.LogWarning("Collapsing selector MapLink {MapLinkId} on map {SourceMapId} to direct travel because only one valid destination ({DestinationMapId}) remains after validation", link.Id, sourceMapId, fallbackDestination.MapId);

                    route = new ResolvedMapLink(link, MapLinkResolutionKind.Direct, fallbackDestination.MapId, fallbackMap, [], UsedFallback: false, DistanceSquared: 0f);
                }
                else
                {
                    route = new ResolvedMapLink(link, MapLinkResolutionKind.Selection, 0, null, selectionDestinations, UsedFallback: false, DistanceSquared: 0f);
                }
            }
            else
            {
                var destinationMapId = destinations[0];
                var destinationMap = await mapRepository.GetByMapIdAsync(destinationMapId, ct);
                if (destinationMap == null)
                {
                    logger.LogWarning("Skipping direct MapLink {MapLinkId} on map {SourceMapId}: destination map {DestinationMapId} was not found", link.Id, sourceMapId, destinationMapId);
                    continue;
                }

                route = new ResolvedMapLink(link, MapLinkResolutionKind.Direct, destinationMapId, destinationMap, [], UsedFallback: false, DistanceSquared: 0f);
            }
            candidates.Add(route.Value);

            var match = ScoreMapLink(link, samples);
            if (match.IsInside)
            {
                if (insideMatch == null || match.DistanceSquared < insideMatch.Value.DistanceSquared)
                    insideMatch = route.Value with { DistanceSquared = match.DistanceSquared };

                continue;
            }

            if (match.IsNear)
            {
                if (nearbyMatch == null || match.DistanceSquared < nearbyMatch.Value.DistanceSquared)
                    nearbyMatch = route.Value with { UsedFallback = true, DistanceSquared = match.DistanceSquared };
            }
        }

        if (insideMatch != null)
            return insideMatch;

        if (nearbyMatch != null)
            return nearbyMatch;

        if (allowSingleLinkFallback && candidates.Count == 1)
            return candidates[0] with { UsedFallback = true };

        return null;
    }

    private static bool RequiresSelection(DAL.Entities.MapLink link, IReadOnlyList<uint> destinations)
    {
        return link.Behavior == DAL.Entities.MapLinkBehavior.ForceSelection || destinations.Count != 1;
    }

    private static uint ResolveIslandId(uint mapId)
    {
        var derivedIsland = (mapId / 100) % 10;
        return derivedIsland switch
        {
            >= 1 and <= 3 => derivedIsland,
            _ => 1u,
        };
    }

    private static string ResolveIslandTitle(uint islandId, IReadOnlyList<DAL.Entities.Map> destinationMaps)
    {
        var baseName = destinationMaps.Select(destinationMap => destinationMap.Name).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));

        if (!string.IsNullOrWhiteSpace(baseName))
        {
            var lastSpace = baseName.LastIndexOf(' ');
            if (lastSpace > 0 && int.TryParse(baseName[(lastSpace + 1)..], out _))
                baseName = baseName[..lastSpace];

            return $"{baseName} Island {islandId}";
        }

        return $"Island {islandId}";
    }

    private static MapLinkMatch ScoreMapLink(DAL.Entities.MapLink link, IReadOnlyList<PositionSample> samples)
    {
        var bestDistanceSquared = float.MaxValue;
        var inside = false;

        foreach (var sample in samples)
        {
            var distanceSquared = MapLinkGeometry.DistanceSquaredToRectangle(link, sample.X, sample.Z);
            if (distanceSquared < bestDistanceSquared)
                bestDistanceSquared = distanceSquared;

            if (MapLinkGeometry.ContainsPoint(link, sample.X, sample.Z))
            {
                inside = true;
                bestDistanceSquared = 0f;
            }
        }

        if (!inside)
        {
            for (var index = 1; index < samples.Count; index++)
            {
                var previous = samples[index - 1];
                var current = samples[index];
                if (!MapLinkGeometry.IntersectsSegment(link, previous.X, previous.Z, current.X, current.Z))
                    continue;

                inside = true;
                bestDistanceSquared = 0f;
                break;
            }
        }

        return new MapLinkMatch(inside, false, bestDistanceSquared);
    }

    public readonly record struct PositionSample(float X, float Z);

    private readonly record struct ResolvedMapLink(DAL.Entities.MapLink Link, MapLinkResolutionKind Kind, uint DestinationMapId, DAL.Entities.Map? DestinationMap, IReadOnlyList<AreaMapSelectionDestination> SelectionDestinations, bool UsedFallback, float DistanceSquared);

    private readonly record struct MapLinkMatch(bool IsInside, bool IsNear, float DistanceSquared);

    private enum MapLinkResolutionKind
    {
        Direct,
        Selection,
    }
}
