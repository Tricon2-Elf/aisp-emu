using AISpace.Common.Config;
using AISpace.Common.DAL.Repositories;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AISpace.Common.Game;

public sealed class DirectMapLinkTransitionService(
    IMapRepository mapRepository,
    ICharacterRepository characterRepository,
    IMyRoomRepository myRoomRepository,
    IMapLinkRepository mapLinkRepository,
    IChannelRepository channelRepository,
    IOptions<ServerOptions> serverOptions,
    SharedState state,
    ILogger<DirectMapLinkTransitionService> logger
)
{
    private const uint SelectorSuccess = 0;
    private const uint SelectorFailure = 1;
    private static readonly ServerInfo SameAreaServerInfo = new("", 0);

    public async Task<DAL.Entities.Character?> ResolveCharacterAsync(
        IPlayerSession session,
        CancellationToken ct = default
    )
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

    public async Task<bool> TryHandleMapEnterTriggerAsync(
        AreaMapEnterRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (session.IsMapTransitionPending)
            return false;

        var character = await ResolveCharacterAsync(session, ct);
        if (character == null)
            return false;

        var resolvedLink = await ResolveTriggeredMapLinkAsync(
            request.MapID,
            request.ChannelId,
            [new PositionSample(session.X, session.Z)],
            ct
        );

        if (resolvedLink == null)
            return false;

        return await ExecuteTriggeredMapLinkAsync(
            "MapEnterRequest",
            request.MapID,
            request.ChannelId,
            session,
            character,
            resolvedLink.Value,
            sendMapEnterResponseForDirect: true,
            sendMapEnterResponseForSelection: true,
            samplesCount: 1,
            ct
        );
    }

    public async Task<bool> TryHandleMovementTriggerAsync(
        IPlayerSession session,
        IReadOnlyList<PositionSample> samples,
        CancellationToken ct = default
    )
    {
        if (
            session.IsMapTransitionPending
            || session.PendingAreaMapSelection != null
            || session.MapId == 0
            || session.ChannelId == 0
            || samples.Count == 0
        )
            return false;

        var character = await ResolveCharacterAsync(session, ct);
        if (character == null)
            return false;

        var resolvedLink = await ResolveTriggeredMapLinkAsync(
            session.MapId,
            (uint)session.ChannelId,
            samples,
            ct
        );

        if (resolvedLink == null)
            return false;

        return await ExecuteTriggeredMapLinkAsync(
            "movement-based",
            session.MapId,
            (uint)session.ChannelId,
            session,
            character,
            resolvedLink.Value,
            sendMapEnterResponseForDirect: false,
            sendMapEnterResponseForSelection: false,
            samplesCount: samples.Count,
            ct
        );
    }

    public async Task<bool> HandleAreaMapSelectionReplyAsync(
        EventAreaMapSelectExecRRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (session.PendingAreaMapSelection == null)
        {
            if (session.IsMapTransitionPending)
            {
                logger.LogInformation(
                    "Ignoring area-map selection reply from user {UserId}: a map transition is already pending on map {MapId}, channel {ChannelId}",
                    session.User?.Id ?? session.UserId,
                    session.MapId,
                    session.ChannelId
                );
                return true;
            }

            logger.LogWarning(
                "Rejecting area-map selection reply from user {UserId}: no selector is pending on map {MapId}, channel {ChannelId}",
                session.User?.Id ?? session.UserId,
                session.MapId,
                session.ChannelId
            );
            await session.SendAsync(
                PacketType.EventAreaMapSelectCloseNotify,
                new EventAreaMapSelectCloseNotify(SelectorFailure).ToBytes(),
                ct
            );
            return true;
        }

        var selection = session.PendingAreaMapSelection;
        session.PendingAreaMapSelection = null;

        if (request.Result != SelectorSuccess)
        {
            logger.LogInformation(
                "Closing area-map selector for user {UserId} with client result {Result}",
                session.User?.Id ?? session.UserId,
                request.Result
            );
            await session.SendAsync(
                PacketType.EventAreaMapSelectCloseNotify,
                new EventAreaMapSelectCloseNotify(request.Result).ToBytes(),
                ct
            );
            return true;
        }

        var selectedDestination = selection.Destinations.FirstOrDefault(destination =>
            destination.MapId == request.MapId && destination.ChannelId == request.ChannelId
        );
        if (selectedDestination == null)
        {
            logger.LogWarning(
                "Rejecting area-map selection reply from user {UserId}: map {MapId}, channel {ChannelId} is not one of the offered destinations for MapLink {MapLinkId}",
                session.User?.Id ?? session.UserId,
                request.MapId,
                request.ChannelId,
                selection.LinkId
            );
            await session.SendAsync(
                PacketType.EventAreaMapSelectCloseNotify,
                new EventAreaMapSelectCloseNotify(SelectorFailure).ToBytes(),
                ct
            );
            return true;
        }

        var character = await ResolveCharacterAsync(session, ct);
        if (character == null)
        {
            logger.LogWarning(
                "Rejecting area-map selection reply from user {UserId}: character could not be resolved",
                session.User?.Id ?? session.UserId
            );
            await session.SendAsync(
                PacketType.EventAreaMapSelectCloseNotify,
                new EventAreaMapSelectCloseNotify(SelectorFailure).ToBytes(),
                ct
            );
            return true;
        }

        var destinationMap = await mapRepository.GetByMapIdAsync(selectedDestination.MapId, ct);
        if (destinationMap == null)
        {
            logger.LogWarning(
                "Rejecting area-map selection reply from user {UserId}: destination map {MapId} was not found",
                session.User?.Id ?? session.UserId,
                selectedDestination.MapId
            );
            await session.SendAsync(
                PacketType.EventAreaMapSelectCloseNotify,
                new EventAreaMapSelectCloseNotify(SelectorFailure).ToBytes(),
                ct
            );
            return true;
        }

        await session.SendAsync(
            PacketType.EventAreaMapSelectCloseNotify,
            new EventAreaMapSelectCloseNotify(SelectorSuccess).ToBytes(),
            ct
        );

        var areaServerInfo = await ResolveAreaServerInfoForNotifyAsync(
            session.ChannelId,
            (int)selectedDestination.ChannelId,
            ct
        );
        var notifyChangeMap = CreateNotifyChangeMap(
            selectedDestination.ChannelId,
            selectedDestination.MapId,
            destinationMap,
            areaServerInfo
        );

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

        await CompleteMapTransitionAsync(
            session,
            character,
            selectedDestination.MapId,
            selectedDestination.ChannelId,
            destinationMap,
            notifyChangeMap,
            sendMapEnterResponse: false,
            ct
        );
        return true;
    }

    public async Task<bool> OpenPendingAreaMapSelectionAsync(
        IPlayerSession session,
        uint selectedIslandId,
        CancellationToken ct = default
    )
    {
        var selection = session.PendingAreaMapSelection;
        if (selection == null)
        {
            logger.LogWarning(
                "Ignoring SelectInitIslandEndRequest from user {UserId}: no selector is pending on map {MapId}, channel {ChannelId}",
                session.User?.Id ?? session.UserId,
                session.MapId,
                session.ChannelId
            );
            return false;
        }

        if (!selection.AwaitingIslandBootstrapAck || selection.SelectorOpened)
        {
            logger.LogInformation(
                "Ignoring duplicate SelectInitIslandEndRequest from user {UserId} for MapLink {MapLinkId}",
                session.User?.Id ?? session.UserId,
                selection.LinkId
            );
            return true;
        }

        var allowedIslandIds = selection
            .Destinations.Select(destination =>
                ResolveIslandId(destination.MapId, selection.IslandId)
            )
            .Append(selection.IslandId)
            .Distinct()
            .ToList();
        var resolvedIslandId = allowedIslandIds.Contains(selectedIslandId)
            ? selectedIslandId
            : selection.IslandId;
        if (resolvedIslandId != selectedIslandId)
        {
            logger.LogWarning(
                "SelectInitIslandEndRequest from user {UserId} acknowledged unknown island {RequestedIslandId} for MapLink {MapLinkId}; falling back to island {IslandId}",
                session.User?.Id ?? session.UserId,
                selectedIslandId,
                selection.LinkId,
                resolvedIslandId
            );
        }

        if (selection.SelectorOpened)
        {
            selection.AwaitingIslandBootstrapAck = false;
            logger.LogInformation(
                "Acknowledged island bootstrap for user {UserId}: selector MapLink {MapLinkId} was already open on island {IslandId}",
                session.User?.Id ?? session.UserId,
                selection.LinkId,
                resolvedIslandId
            );
            return true;
        }

        var selectorEntries = await CreateSelectorEntriesAsync(selection, ct);
        if (selectorEntries.Count == 0)
        {
            logger.LogWarning(
                "Aborting selector MapLink {MapLinkId} for user {UserId}: no valid selector entries could be built after island bootstrap",
                selection.LinkId,
                session.User?.Id ?? session.UserId
            );
            session.PendingAreaMapSelection = null;
            return false;
        }

        selection.AwaitingIslandBootstrapAck = false;
        selection.SelectorOpened = true;

        logger.LogInformation(
            "Opening area-map selector for user {UserId}: MapLink {MapLinkId} with {DestinationCount} destination(s) on island {IslandId}",
            session.User?.Id ?? session.UserId,
            selection.LinkId,
            selectorEntries.Count,
            resolvedIslandId
        );

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

    public async Task<bool> TryTeleportToMapAsync(
        IPlayerSession session,
        uint destinationMapId,
        CancellationToken ct = default
    )
    {
        var character = await ResolveCharacterAsync(session, ct);
        if (character == null)
            return false;

        if (MyRoomInfo.IsMyRoomMap(destinationMapId))
        {
            var room = await myRoomRepository.GetOrCreateDefaultRoomAsync(character.Id, ct);
            return room is not null && await TryTeleportToRoomAsync(session, room, ct);
        }

        var destinationMap = await mapRepository.GetByMapIdAsync(destinationMapId, ct);
        if (destinationMap == null)
            return false;

        var channelId = await ResolveChannelIdForMapAsync(destinationMapId, ct);
        if (channelId == null)
            return false;

        var areaServerInfo = await ResolveAreaServerInfoForNotifyAsync(
            session.ChannelId,
            channelId.Value,
            ct
        );
        var notifyChangeMap =
            destinationMapId == session.MapId
                ? null
                : CreateNotifyChangeMap(
                    (uint)channelId.Value,
                    destinationMapId,
                    destinationMap,
                    areaServerInfo
                );

        await CompleteMapTransitionAsync(
            session,
            character,
            destinationMapId,
            (uint)channelId.Value,
            destinationMap,
            notifyChangeMap,
            sendMapEnterResponse: notifyChangeMap == null,
            ct
        );
        return true;
    }

    public async Task<bool> TryTeleportToRoomAsync(
        IPlayerSession session,
        DAL.Entities.Room room,
        CancellationToken ct = default
    )
    {
        if (room.Id <= 0 || !Enum.IsDefined(room.Stage))
            return false;

        var character = await ResolveCharacterAsync(session, ct);
        if (character == null)
            return false;

        var destinationMapId = MyRoomInfo.GetMapId(room.Stage);
        var destinationMap = await mapRepository.GetByMapIdAsync(destinationMapId, ct);
        if (destinationMap == null)
            return false;

        var channelId = await ResolveChannelIdForMapAsync(destinationMapId, ct);
        if (channelId == null)
            return false;

        var areaServerInfo = await ResolveAreaServerInfoForNotifyAsync(
            session.ChannelId,
            channelId.Value,
            ct
        );
        var shouldReload =
            destinationMapId != session.MapId || session.MyRoomId != checked((uint)room.Id);
        var notifyChangeMap = shouldReload
            ? CreateNotifyChangeMap(
                (uint)channelId.Value,
                destinationMapId,
                destinationMap,
                areaServerInfo
            )
            : null;

        await CompleteMapTransitionAsync(
            session,
            character,
            destinationMapId,
            (uint)channelId.Value,
            destinationMap,
            notifyChangeMap,
            sendMapEnterResponse: notifyChangeMap == null,
            ct,
            room
        );
        return true;
    }

    public async Task<NotifyChangeMap> BuildNotifyChangeMapAsync(
        uint channelId,
        uint destinationMapId,
        DAL.Entities.Map destinationMap,
        int sourceChannelId = 0,
        CancellationToken ct = default
    )
    {
        var areaServerInfo = await ResolveAreaServerInfoForNotifyAsync(
            sourceChannelId,
            (int)channelId,
            ct
        );
        return CreateNotifyChangeMap(channelId, destinationMapId, destinationMap, areaServerInfo);
    }

    private async Task<int?> ResolveChannelIdForMapAsync(
        uint destinationMapId,
        CancellationToken ct
    )
    {
        var channels = await channelRepository.GetAllAsync(ct);
        if (channels.Count == 0)
            return null;

        var exactMatch = channels
            .Where(channel => channel.MapId == destinationMapId)
            .OrderBy(channel => channel.ChannelNum)
            .FirstOrDefault();
        if (exactMatch != null)
            return exactMatch.ChannelNum;

        var mapGroup = destinationMapId / 10_000u;
        var groupMatch = channels
            .Where(channel => channel.MapId / 10_000u == mapGroup)
            .OrderBy(channel => channel.ChannelNum)
            .FirstOrDefault();
        return groupMatch?.ChannelNum
            ?? channels.OrderBy(channel => channel.ChannelNum).First().ChannelNum;
    }

    public async Task CompleteMapTransitionAsync(
        IPlayerSession session,
        DAL.Entities.Character character,
        uint destinationMapId,
        uint destinationChannelId,
        DAL.Entities.Map destinationMap,
        NotifyChangeMap? notifyChangeMap,
        bool sendMapEnterResponse,
        CancellationToken ct = default,
        DAL.Entities.Room? destinationRoom = null
    )
    {
        if (MyRoomInfo.IsMyRoomMap(destinationMapId))
        {
            destinationRoom ??= await myRoomRepository.GetOrCreateDefaultRoomAsync(
                character.Id,
                ct
            );
            if (destinationRoom is null)
            {
                logger.LogWarning(
                    "Cannot enter MyRoom map {MapId} for character {CharacterId}: no room could be resolved",
                    destinationMapId,
                    character.Id
                );
                return;
            }

            var roomMapId = MyRoomInfo.GetMapId(destinationRoom.Stage);
            if (roomMapId != destinationMapId)
            {
                var roomMap = await mapRepository.GetByMapIdAsync(roomMapId, ct);
                var roomChannelId = await ResolveChannelIdForMapAsync(roomMapId, ct);
                if (roomMap is null || roomChannelId is null)
                {
                    logger.LogWarning(
                        "Cannot enter room {RoomId}: map {MapId} for stage {Stage} is unavailable",
                        destinationRoom.Id,
                        roomMapId,
                        destinationRoom.Stage
                    );
                    return;
                }

                var roomAreaServerInfo = await ResolveAreaServerInfoForNotifyAsync(
                    session.ChannelId,
                    roomChannelId.Value,
                    ct
                );
                var roomNotify = CreateNotifyChangeMap(
                    (uint)roomChannelId.Value,
                    roomMapId,
                    roomMap,
                    roomAreaServerInfo
                );
                await CompleteMapTransitionAsync(
                    session,
                    character,
                    roomMapId,
                    (uint)roomChannelId.Value,
                    roomMap,
                    roomNotify,
                    sendMapEnterResponse: false,
                    ct,
                    destinationRoom
                );
                return;
            }
        }

        if (notifyChangeMap != null)
            session.IsMapTransitionPending = true;

        var sourceChannelId = session.ChannelId;

        await state.BroadcastAreaDisappearAsync(session, ct);
        await state.ClearRemoteRobosAsync(session, ct);

        var updatedCharacter =
            await characterRepository.UpdateCurrentLocationAsync(
                character.Id,
                destinationMapId,
                destinationRoom?.Id,
                ct
            ) ?? character;
        updatedCharacter.CurrentMapId = destinationMapId;
        updatedCharacter.CurrentRoomId = destinationRoom?.Id;

        var spawnX = notifyChangeMap?.PositionX ?? destinationMap.SpawnX;
        var spawnY = notifyChangeMap?.PositionY ?? destinationMap.SpawnY;
        var spawnZ = notifyChangeMap?.PositionZ ?? destinationMap.SpawnZ;
        var spawnRotation = notifyChangeMap?.Rotation ?? destinationMap.SpawnRotation;

        session.Character = updatedCharacter;
        session.CharacterId = (uint)updatedCharacter.Id;
        session.MapId = destinationMapId;
        session.MyRoomId = destinationRoom is null ? 0 : checked((uint)destinationRoom.Id);
        session.PendingMyRoomFurnitureItemId = null;
        session.ChannelId = (int)destinationChannelId;
        session.X = spawnX;
        session.Y = spawnY;
        session.Z = spawnZ;
        session.Rotation = spawnRotation;
        session.MovementTypeId = (int)MovementType.Stopped;
        session.HasMovedSinceMapLoad = false;
        session.IsMapTransitionPending = notifyChangeMap != null;
        session.PendingAreaMapSelection = null;

        state.RegisterClient(ServerType.Area, session);

        var userCharacter = session.User?.Characters.FirstOrDefault(candidate =>
            candidate.Id == updatedCharacter.Id
        );
        if (userCharacter != null)
        {
            userCharacter.CurrentMapId = destinationMapId;
            userCharacter.CurrentRoomId = destinationRoom?.Id;
        }

        if (notifyChangeMap != null)
        {
            session.NeedsPostLoadSelfAvatarNotify = true;

            if (
                session.User != null
                && await RequiresAreaServerReconnectAsync(
                    sourceChannelId,
                    (int)destinationChannelId,
                    ct
                )
            )
            {
                state.SetPendingAreaTransition(
                    new SharedState.PendingMapTransfer(
                        session.User.Id,
                        destinationMapId,
                        (int)destinationChannelId,
                        spawnX,
                        spawnY,
                        spawnZ,
                        spawnRotation,
                        session.MyRoomId
                    )
                );
            }
        }

        if (sendMapEnterResponse)
            await session.SendAsync(
                PacketType.MapEnterResponse,
                new AreaMapEnterResponse(0).ToBytes(),
                ct
            );

        if (notifyChangeMap != null)
        {
            if (MyRoomInfo.IsMyRoomMap(destinationMapId))
            {
                // MyRoom maps must be entered through recv_notify_change_myroom: it flips the client's
                // "in MyRoom" flag and stores the room owner info, without which the door/closet
                // furniture notifies (recv_notify_myroom_furniture) are ignored by the client.
                var notifyChangeMyRoom = new NotifyChangeMyRoom
                {
                    ChannelId = notifyChangeMap.ChannelId,
                    MapId = notifyChangeMap.MapId,
                    MapSerialId = notifyChangeMap.MapSerialId,
                    RouteState = notifyChangeMap.RouteState,
                    PositionX = notifyChangeMap.PositionX,
                    PositionY = notifyChangeMap.PositionY,
                    PositionZ = notifyChangeMap.PositionZ,
                    Rotation = notifyChangeMap.Rotation,
                    Animation = notifyChangeMap.Animation,
                    Flag = notifyChangeMap.Flag,
                    AreaServerInfo = notifyChangeMap.AreaServerInfo,
                    Room = new MyRoomData(
                        checked((uint)destinationRoom!.Id),
                        checked((uint)destinationRoom.OwnerCharacterId),
                        destinationRoom.Stage,
                        destinationRoom.Name,
                        destinationRoom.Security
                    ),
                    FadeFlag = notifyChangeMap.FadeFlag,
                };

                logger.LogInformation(
                    "Sending NotifyChangeMyRoom for user {UserId} to room {RoomId} on map {MapId} (stage {Stage}, owner character {OwnerCharacterId})",
                    session.User?.Id ?? session.UserId,
                    notifyChangeMyRoom.Room.RoomId,
                    destinationMapId,
                    notifyChangeMyRoom.Room.RoomStage,
                    notifyChangeMyRoom.Room.OwnerCharacterId
                );
                await session.SendAsync(
                    PacketType.NotifyChangeMyRoom,
                    notifyChangeMyRoom.ToBytes(),
                    ct
                );
            }
            else
            {
                await session.SendAsync(PacketType.NotifyChangeMap, notifyChangeMap.ToBytes(), ct);
            }
        }
    }

    private async Task<ServerInfo> ResolveAreaServerInfoAsync(int channelId, CancellationToken ct)
    {
        var currentChannel = await channelRepository.GetByChannelNumAsync(channelId, ct);
        if (currentChannel == null)
        {
            var port = (ushort)serverOptions.Value.AreaServer.Port;
            logger.LogWarning(
                "Channel {ChannelId} was not found while building NotifyChangeMap; falling back to localhost:{Port}",
                channelId,
                port
            );
            return new ServerInfo(serverOptions.Value.ResolveAddress("localhost"), port);
        }

        return new ServerInfo(
            serverOptions.Value.ResolveAddress(currentChannel.IP),
            currentChannel.Port
        );
    }

    private async Task<bool> RequiresAreaServerReconnectAsync(
        int sourceChannelId,
        int destinationChannelId,
        CancellationToken ct
    )
    {
        if (sourceChannelId == 0)
            return false;

        var currentAreaServer = await ResolveAreaServerInfoAsync(sourceChannelId, ct);
        var destinationAreaServer = await ResolveAreaServerInfoAsync(destinationChannelId, ct);
        return !string.Equals(
                currentAreaServer.IP,
                destinationAreaServer.IP,
                StringComparison.OrdinalIgnoreCase
            )
            || currentAreaServer.Port != destinationAreaServer.Port;
    }

    private async Task<ServerInfo> ResolveAreaServerInfoForNotifyAsync(
        int sourceChannelId,
        int destinationChannelId,
        CancellationToken ct
    )
    {
        if (!await RequiresAreaServerReconnectAsync(sourceChannelId, destinationChannelId, ct))
            return SameAreaServerInfo;

        return await ResolveAreaServerInfoAsync(destinationChannelId, ct);
    }

    private NotifyChangeMap CreateNotifyChangeMap(
        uint channelId,
        uint destinationMapId,
        DAL.Entities.Map destinationMap,
        ServerInfo areaServerInfo,
        DAL.Entities.MapLink? link = null
    )
    {
        var (spawnX, spawnY, spawnZ, spawnRotation) =
            link?.ResolveDestinationSpawn(destinationMap)
            ?? (
                destinationMap.SpawnX,
                destinationMap.SpawnY,
                destinationMap.SpawnZ,
                destinationMap.SpawnRotation
            );

        return new NotifyChangeMap
        {
            ChannelId = channelId,
            MapId = destinationMapId,
            MapSerialId = destinationMapId,
            RouteState = 0,
            PositionX = spawnX,
            PositionY = spawnY,
            PositionZ = spawnZ,
            Rotation = spawnRotation,
            Animation = (byte)MovementType.Stopped,
            // Decompiled transition handling checks bit 0x2 on both flag bytes.
            Flag = 0,
            AreaServerInfo = areaServerInfo,
            FadeFlag = 0,
        };
    }

    private NotifySelectMapEntry CreateSelectMapEntry(
        uint channelId,
        uint destinationMapId,
        DAL.Entities.Map destinationMap,
        ServerInfo areaServerInfo,
        DAL.Entities.MapLink? link = null
    )
    {
        var (spawnX, spawnY, spawnZ, spawnRotation) =
            link?.ResolveDestinationSpawn(destinationMap)
            ?? (
                destinationMap.SpawnX,
                destinationMap.SpawnY,
                destinationMap.SpawnZ,
                destinationMap.SpawnRotation
            );

        return new NotifySelectMapEntry
        {
            MapId = destinationMapId,
            AreaServerInfo = areaServerInfo,
            ChannelId = channelId,
            RouteMapId = destinationMapId,
            MapSerialId = destinationMapId,
            RouteState = 0,
            PositionX = spawnX,
            PositionY = spawnY,
            PositionZ = spawnZ,
            Yaw = spawnRotation,
            Animation = (byte)MovementType.Stopped,
            Unknown1 = 0,
            Unknown2 = 0,
        };
    }

    private async Task<bool> ExecuteTriggeredMapLinkAsync(
        string triggerName,
        uint sourceMapId,
        uint channelId,
        IPlayerSession session,
        DAL.Entities.Character character,
        ResolvedMapLink resolvedLink,
        bool sendMapEnterResponseForDirect,
        bool sendMapEnterResponseForSelection,
        int samplesCount,
        CancellationToken ct
    )
    {
        if (resolvedLink.Kind == MapLinkResolutionKind.Direct)
        {
            var areaServerInfo = await ResolveAreaServerInfoForNotifyAsync(
                session.ChannelId,
                (int)channelId,
                ct
            );
            var notifyChangeMap = CreateNotifyChangeMap(
                channelId,
                resolvedLink.DestinationMapId,
                resolvedLink.DestinationMap!,
                areaServerInfo,
                resolvedLink.Link
            );

            logger.LogInformation(
                "Resolved {TriggerName} MapLink trigger on map {SourceMapId} at position ({X}, {Y}, {Z}) to MapLink {MapLinkId} -> destination map {DestinationMapId} at ({DestX}, {DestY}, {DestZ}) via {Ip}:{Port} (channel {ChannelId}, flag {Flag}, fade {FadeFlag}){SampleSuffix}{FallbackSuffix}",
                triggerName,
                sourceMapId,
                session.X,
                session.Y,
                session.Z,
                resolvedLink.Link.Id,
                resolvedLink.DestinationMapId,
                notifyChangeMap.PositionX,
                notifyChangeMap.PositionY,
                notifyChangeMap.PositionZ,
                areaServerInfo.IP,
                areaServerInfo.Port,
                channelId,
                notifyChangeMap.Flag,
                notifyChangeMap.FadeFlag,
                triggerName == "movement-based"
                    ? $" using {samplesCount} movement samples"
                    : string.Empty,
                resolvedLink.UsedFallback ? " using fallback resolution" : string.Empty
            );

            await CompleteMapTransitionAsync(
                session,
                character,
                resolvedLink.DestinationMapId,
                channelId,
                resolvedLink.DestinationMap!,
                notifyChangeMap,
                sendMapEnterResponseForDirect,
                ct
            );
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

        if (
            session.PendingAreaMapSelection is { } existingSelection
            && existingSelection.LinkId == selection.LinkId
            && existingSelection.SourceMapId == selection.SourceMapId
            && existingSelection.ChannelId == selection.ChannelId
        )
        {
            if (sendMapEnterResponseForSelection)
                await session.SendAsync(
                    PacketType.MapEnterResponse,
                    new AreaMapEnterResponse(0).ToBytes(),
                    ct
                );

            return true;
        }

        session.PendingAreaMapSelection = selection;
        session.HasMovedSinceMapLoad = true;

        if (sendMapEnterResponseForSelection)
            await session.SendAsync(
                PacketType.MapEnterResponse,
                new AreaMapEnterResponse(0).ToBytes(),
                ct
            );

        logger.LogInformation(
            "Resolved {TriggerName} MapLink trigger on map {SourceMapId} at position ({X}, {Y}, {Z}) to selector MapLink {MapLinkId} with {DestinationCount} destination(s){SampleSuffix}{FallbackSuffix}",
            triggerName,
            sourceMapId,
            session.X,
            session.Y,
            session.Z,
            resolvedLink.Link.Id,
            selection.Destinations.Count,
            triggerName == "movement-based"
                ? $" using {samplesCount} movement samples"
                : string.Empty,
            resolvedLink.UsedFallback ? " using fallback resolution" : string.Empty
        );

        var selectorEntries = await CreateSelectorEntriesAsync(selection, ct);
        if (selectorEntries.Count == 0)
        {
            logger.LogWarning(
                "Aborting selector MapLink {MapLinkId} for user {UserId}: no valid selector entries could be built",
                selection.LinkId,
                session.User?.Id ?? session.UserId
            );
            session.PendingAreaMapSelection = null;
            return false;
        }

        selection.SelectorOpened = true;

        await session.SendAsync(
            PacketType.SelectInitIslandStart,
            (await CreateSelectInitIslandStartAsync(selection, ct)).ToBytes(),
            ct
        );
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

    private async Task<List<NotifySelectMapEntry>> CreateSelectorEntriesAsync(
        PendingAreaMapSelection selection,
        CancellationToken ct
    )
    {
        var selectorEntries = new List<NotifySelectMapEntry>(selection.Destinations.Count);
        foreach (var destination in selection.Destinations)
        {
            var destinationMap = await mapRepository.GetByMapIdAsync(destination.MapId, ct);
            if (destinationMap == null)
            {
                logger.LogWarning(
                    "Skipping selector entry for MapLink {MapLinkId}: destination map {DestinationMapId} was not found while building EventAreaMapSelectExec",
                    selection.LinkId,
                    destination.MapId
                );
                continue;
            }

            var areaServerInfo = await ResolveAreaServerInfoAsync((int)destination.ChannelId, ct);
            selectorEntries.Add(
                CreateSelectMapEntry(
                    destination.ChannelId,
                    destination.MapId,
                    destinationMap,
                    areaServerInfo
                )
            );
        }

        return selectorEntries;
    }

    private async Task<SelectInitIslandStartNotify> CreateSelectInitIslandStartAsync(
        PendingAreaMapSelection selection,
        CancellationToken ct
    )
    {
        // Non-franchise maps (My Room, Akihabara, …) inherit the source map's island so the selector chrome matches the area you opened it from.
        var islandIds = selection
            .Destinations.Select(destination =>
                ResolveIslandId(destination.MapId, selection.IslandId)
            )
            .Append(selection.IslandId)
            .Distinct()
            .OrderBy(islandId => islandId)
            .ToList();

        var islands = new List<SelectInitIslandEntry>(islandIds.Count);
        foreach (var islandId in islandIds)
        {
            var destinationMaps = new List<DAL.Entities.Map>();
            foreach (
                var destination in selection.Destinations.Where(destination =>
                    ResolveIslandId(destination.MapId, selection.IslandId) == islandId
                )
            )
            {
                var destinationMap = await mapRepository.GetByMapIdAsync(destination.MapId, ct);
                if (destinationMap != null)
                    destinationMaps.Add(destinationMap);
            }

            var title = ResolveIslandTitle(islandId, destinationMaps);
            var descriptionLines = destinationMaps
                .Select(destinationMap => destinationMap.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .ToList();

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

    private async Task<ResolvedMapLink?> ResolveTriggeredMapLinkAsync(
        uint sourceMapId,
        uint channelId,
        IReadOnlyList<PositionSample> samples,
        CancellationToken ct
    )
    {
        var links = await mapLinkRepository.GetBySourceMapAsync(sourceMapId, channelId, ct);
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
                var selectionDestinations = new List<AreaMapSelectionDestination>(
                    destinations.Count
                );
                foreach (var destinationMapId in destinations)
                {
                    var destinationMap = await mapRepository.GetByMapIdAsync(destinationMapId, ct);
                    if (destinationMap == null)
                    {
                        logger.LogWarning(
                            "Skipping selector destination map {DestinationMapId} for MapLink {MapLinkId} on map {SourceMapId}: destination map was not found",
                            destinationMapId,
                            link.Id,
                            sourceMapId
                        );
                        continue;
                    }

                    selectionDestinations.Add(
                        new AreaMapSelectionDestination(destinationMapId, channelId)
                    );
                }

                if (selectionDestinations.Count == 0)
                    continue;

                if (selectionDestinations.Count == 1)
                {
                    var fallbackDestination = selectionDestinations[0];
                    var fallbackMap = await mapRepository.GetByMapIdAsync(
                        fallbackDestination.MapId,
                        ct
                    );
                    if (fallbackMap == null)
                        continue;

                    logger.LogWarning(
                        "Collapsing selector MapLink {MapLinkId} on map {SourceMapId} to direct travel because only one valid destination ({DestinationMapId}) remains after validation",
                        link.Id,
                        sourceMapId,
                        fallbackDestination.MapId
                    );

                    route = new ResolvedMapLink(
                        link,
                        MapLinkResolutionKind.Direct,
                        fallbackDestination.MapId,
                        fallbackMap,
                        [],
                        UsedFallback: false,
                        DistanceSquared: 0f
                    );
                }
                else
                {
                    route = new ResolvedMapLink(
                        link,
                        MapLinkResolutionKind.Selection,
                        0,
                        null,
                        selectionDestinations,
                        UsedFallback: false,
                        DistanceSquared: 0f
                    );
                }
            }
            else
            {
                var destinationMapId = destinations[0];
                var destinationMap = await mapRepository.GetByMapIdAsync(destinationMapId, ct);
                if (destinationMap == null)
                {
                    logger.LogWarning(
                        "Skipping direct MapLink {MapLinkId} on map {SourceMapId}: destination map {DestinationMapId} was not found",
                        link.Id,
                        sourceMapId,
                        destinationMapId
                    );
                    continue;
                }

                route = new ResolvedMapLink(
                    link,
                    MapLinkResolutionKind.Direct,
                    destinationMapId,
                    destinationMap,
                    [],
                    UsedFallback: false,
                    DistanceSquared: 0f
                );
            }

            var match = ScoreMapLink(link, samples);
            if (match.IsInside)
            {
                if (
                    insideMatch == null
                    || match.DistanceSquared < insideMatch.Value.DistanceSquared
                )
                    insideMatch = route.Value with { DistanceSquared = match.DistanceSquared };

                continue;
            }

            if (match.IsNear)
            {
                if (
                    nearbyMatch == null
                    || match.DistanceSquared < nearbyMatch.Value.DistanceSquared
                )
                    nearbyMatch = route.Value with
                    {
                        UsedFallback = true,
                        DistanceSquared = match.DistanceSquared,
                    };
            }
        }

        if (insideMatch != null)
            return insideMatch;

        if (nearbyMatch != null)
            return nearbyMatch;

        return null;
    }

    private static bool RequiresSelection(
        DAL.Entities.MapLink link,
        IReadOnlyList<uint> destinations
    )
    {
        return link.Behavior == DAL.Entities.MapLinkBehavior.ForceSelection
            || destinations.Count != 1;
    }

    /// <summary>
    /// Franchise island from map id (same encoding as shinju registration): 1001xxxx → 1 Da Capo, 1002xxxx → 2 Clannad, 1003xxxx → 3 Shuffle.
    /// Non-franchise maps (My Room, Akihabara, …) use <paramref name="fallbackIslandId"/>.
    /// </summary>
    private static uint ResolveIslandId(uint mapId, uint fallbackIslandId = 1u)
    {
        var derivedIsland = (mapId / 10_000u) % 100u;
        return derivedIsland is >= 1 and <= 3 ? derivedIsland : fallbackIslandId;
    }

    private static string ResolveIslandTitle(
        uint islandId,
        IReadOnlyList<DAL.Entities.Map> destinationMaps
    )
    {
        var baseName = destinationMaps
            .Select(destinationMap => destinationMap.Name)
            .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));

        if (!string.IsNullOrWhiteSpace(baseName))
        {
            var lastSpace = baseName.LastIndexOf(' ');
            if (lastSpace > 0 && int.TryParse(baseName[(lastSpace + 1)..], out _))
                baseName = baseName[..lastSpace];

            return $"{baseName} Island {islandId}";
        }

        return $"Island {islandId}";
    }

    /// <summary>
    /// Matches only on an outside→inside entry (or a movement segment that crosses the volume).
    /// Standing/spawning inside without an entry edge does not trigger, which prevents portal bounce-back.
    /// </summary>
    private static MapLinkMatch ScoreMapLink(
        DAL.Entities.MapLink link,
        IReadOnlyList<PositionSample> samples
    )
    {
        var bestDistanceSquared = float.MaxValue;

        foreach (var sample in samples)
        {
            var distanceSquared = MapLinkGeometry.DistanceSquaredToRectangle(
                link,
                sample.X,
                sample.Z
            );
            if (distanceSquared < bestDistanceSquared)
                bestDistanceSquared = distanceSquared;
        }

        for (var index = 1; index < samples.Count; index++)
        {
            var previous = samples[index - 1];
            var current = samples[index];
            var previousInside = MapLinkGeometry.ContainsPoint(link, previous.X, previous.Z);
            var currentInside = MapLinkGeometry.ContainsPoint(link, current.X, current.Z);

            if (!previousInside && currentInside)
                return new MapLinkMatch(true, false, 0f);

            // Large steps from outside can skip the interior while still crossing the volume.
            // Do not treat inside→outside exits (or inside→inside walks) as entries.
            if (
                !previousInside
                && MapLinkGeometry.IntersectsSegment(
                    link,
                    previous.X,
                    previous.Z,
                    current.X,
                    current.Z
                )
            )
                return new MapLinkMatch(true, false, 0f);
        }

        return new MapLinkMatch(false, false, bestDistanceSquared);
    }

    public readonly record struct PositionSample(float X, float Z);

    private readonly record struct ResolvedMapLink(
        DAL.Entities.MapLink Link,
        MapLinkResolutionKind Kind,
        uint DestinationMapId,
        DAL.Entities.Map? DestinationMap,
        IReadOnlyList<AreaMapSelectionDestination> SelectionDestinations,
        bool UsedFallback,
        float DistanceSquared
    );

    private readonly record struct MapLinkMatch(bool IsInside, bool IsNear, float DistanceSquared);

    private enum MapLinkResolutionKind
    {
        Direct,
        Selection,
    }
}
