using System.Collections.Concurrent;
using System.Threading.Channels;
using aisp.Common.DAL.Repositories;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Game;

public class SharedState
{
    private static readonly ServerType[] AuthAndMsgServerTypes = [ServerType.Auth, ServerType.Msg];

    private readonly ISessionStore _sessionStore;
    private readonly ISessionClientRegistry _sessionClientRegistry;
    private readonly IAreaPresenceIndex _areaPresenceIndex;
    private readonly IPendingTransitionStore _pendingTransitionStore;
    private readonly ISessionPresenceRepository? _sessionPresenceRepository;
    private readonly IPendingMapTransferRepository? _pendingMapTransferRepository;
    private readonly ConcurrentDictionary<
        (uint OwnerCharacterId, uint RoboId),
        MovementData
    > _roboLastMovement = new();

    private readonly ConcurrentDictionary<Guid, int> _circleChatSessions = new();
    private readonly ConcurrentDictionary<int, ActivePlacardComment> _activePlacardComments = new();
    private readonly object _friendLinkPlacardLock = new();
    private readonly Dictionary<uint, ActiveFriendLinkPlacard> _friendLinkPlacardsById = [];
    private readonly Dictionary<uint, uint> _friendLinkPlacardIdsByOwner = [];
    private uint _nextFriendLinkPlacardId = 100_000;

    private readonly Channel<(string Id, string Message)> _messages = Channel.CreateUnbounded<(
        string Id,
        string Message
    )>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public IReadOnlyCollection<IPlayerSession> AuthClients => GetServerClients(ServerType.Auth);
    public IReadOnlyCollection<IPlayerSession> MsgClients => GetServerClients(ServerType.Msg);
    public IReadOnlyCollection<IPlayerSession> AreaClients => GetServerClients(ServerType.Area);

    public readonly long StartTimeUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public SharedState()
        : this(new SessionStore(), new SessionClientRegistry(), new PendingTransitionStore()) { }

    public SharedState(
        ISessionStore sessionStore,
        ISessionClientRegistry sessionClientRegistry,
        IPendingTransitionStore pendingTransitionStore
    )
    {
        _sessionStore = sessionStore;
        _sessionClientRegistry = sessionClientRegistry;
        _pendingTransitionStore = pendingTransitionStore;
        _areaPresenceIndex = new AreaPresenceIndex(_sessionClientRegistry);
    }

    public SharedState(
        ISessionStore sessionStore,
        ISessionClientRegistry sessionClientRegistry,
        IPendingTransitionStore pendingTransitionStore,
        ISessionPresenceRepository sessionPresenceRepository,
        IPendingMapTransferRepository pendingMapTransferRepository
    )
        : this(sessionStore, sessionClientRegistry, pendingTransitionStore)
    {
        _sessionPresenceRepository = sessionPresenceRepository;
        _pendingMapTransferRepository = pendingMapTransferRepository;
    }

    /// <summary>Gets or creates a session for the given connection id. Factory is invoked only when a new session is needed.</summary>
    public IPlayerSession GetOrAddSession(Guid connectionId, Func<IPlayerSession> createSession) =>
        _sessionStore.GetOrAddSession(connectionId, createSession);

    public bool TryGetSession(Guid connectionId, out IPlayerSession? session) =>
        _sessionStore.TryGetSession(connectionId, out session);

    /// <summary>
    /// Drops every Auth/Msg/Area socket for this user except <paramref name="exceptConnectionId"/>.
    /// Used on relogin so a half-open connection cannot keep a client-handler slot.
    /// </summary>
    public void DisconnectOtherConnectionsForUser(int userId, Guid exceptConnectionId)
    {
        if (userId <= 0)
            return;

        List<(ServerType Type, IPlayerSession Session)> stale = [];
        foreach (var serverType in AuthAndMsgServerTypes)
        {
            foreach (var existing in GetServerClients(serverType))
            {
                if (existing.ConnectionId == exceptConnectionId)
                    continue;
                if (!BelongsToUser(existing, userId))
                    continue;
                stale.Add((serverType, existing));
            }
        }

        foreach (var (serverType, existing) in stale)
            CloseAndUnregister(serverType, existing);
    }

    public void RegisterClient(ServerType serverType, IPlayerSession session)
    {
        CloseSupersededClients(serverType, session);

        if (_sessionPresenceRepository == null)
        {
            _sessionClientRegistry.Register(serverType, session);
            return;
        }

        _sessionPresenceRepository.Upsert(serverType, session);
    }

    public void UnregisterClient(ServerType serverType, Guid clientId)
    {
        IPlayerSession? areaSession = null;
        IReadOnlyList<IPlayerSession> placardViewers = [];
        ActiveFriendLinkPlacard? removedPlacard = null;
        if (serverType == ServerType.Area)
        {
            if (!_sessionStore.TryGetSession(clientId, out areaSession) || areaSession is null)
                areaSession = GetServerClients(serverType)
                    .FirstOrDefault(x => x.ConnectionId == clientId);
            if (areaSession is not null && areaSession.CharacterId != 0)
            {
                placardViewers = GetAreaPeers(areaSession);
                TryRemoveFriendLinkPlacard(areaSession.CharacterId, out removedPlacard);
            }
        }

        _circleChatSessions.TryRemove(clientId, out _);

        if (_sessionPresenceRepository == null)
            _sessionClientRegistry.Unregister(clientId);
        else
            _sessionPresenceRepository.Remove(serverType, clientId);

        _sessionStore.RemoveSession(clientId);

        if (removedPlacard is not null && placardViewers.Count > 0)
            _ = NotifyPlacardRemoveToPeersAsync(removedPlacard.PlacardId, placardViewers);
    }

    public void EnterCircleChat(Guid connectionId, int circleId) =>
        _circleChatSessions[connectionId] = circleId;

    public bool TryGetCircleChat(Guid connectionId, out int circleId) =>
        _circleChatSessions.TryGetValue(connectionId, out circleId);

    public bool LeaveCircleChat(Guid connectionId) =>
        _circleChatSessions.TryRemove(connectionId, out _);

    public void BeginPlacardComment(int userId, uint placardId)
    {
        if (userId > 0)
            _activePlacardComments[userId] = new ActivePlacardComment(
                placardId,
                DateTime.UtcNow.AddMinutes(2)
            );
    }

    public bool TryTakePlacardComment(int userId, out uint placardId)
    {
        placardId = 0;
        if (userId <= 0 || !_activePlacardComments.TryRemove(userId, out var active))
            return false;
        if (active.ExpiresAtUtc < DateTime.UtcNow)
            return false;

        placardId = active.PlacardId;
        return true;
    }

    public (
        ActiveFriendLinkPlacard Placard,
        ActiveFriendLinkPlacard? Replaced
    ) SetFriendLinkPlacard(
        int ownerUserId,
        uint ownerCharacterId,
        string ownerName,
        uint mapId,
        int channelId,
        uint myRoomId,
        uint tagId,
        uint slot,
        byte direction,
        string tagName,
        System.Numerics.Vector3 position
    )
    {
        lock (_friendLinkPlacardLock)
        {
            ActiveFriendLinkPlacard? replaced = null;
            if (
                _friendLinkPlacardIdsByOwner.Remove(ownerCharacterId, out var previousId)
                && _friendLinkPlacardsById.Remove(previousId, out var previous)
            )
                replaced = previous;

            uint placardId;
            do
            {
                placardId = ++_nextFriendLinkPlacardId;
                if (placardId == 0)
                    placardId = ++_nextFriendLinkPlacardId;
            } while (_friendLinkPlacardsById.ContainsKey(placardId));

            var placard = new ActiveFriendLinkPlacard(
                placardId,
                ownerUserId,
                ownerCharacterId,
                ownerName,
                mapId,
                channelId,
                myRoomId,
                tagId,
                slot,
                direction,
                tagName,
                position
            );
            _friendLinkPlacardsById[placardId] = placard;
            _friendLinkPlacardIdsByOwner[ownerCharacterId] = placardId;
            replaced?.ClearComments();
            RemovePlacardCommentContexts(replaced?.PlacardId);
            return (placard, replaced);
        }
    }

    public ActiveFriendLinkPlacard? GetFriendLinkPlacard(uint placardId)
    {
        lock (_friendLinkPlacardLock)
            return _friendLinkPlacardsById.GetValueOrDefault(placardId);
    }

    public IReadOnlyList<ActiveFriendLinkPlacard> GetFriendLinkPlacards(
        uint mapId,
        int channelId,
        uint myRoomId
    )
    {
        lock (_friendLinkPlacardLock)
            return
            [
                .. _friendLinkPlacardsById
                    .Values.Where(x =>
                        x.MapId == mapId
                        && x.ChannelId == channelId
                        && (myRoomId == 0 ? x.MyRoomId == 0 : x.MyRoomId == myRoomId)
                    )
                    .OrderBy(x => x.PlacardId)
                    .Take(300),
            ];
    }

    public bool TryRemoveFriendLinkPlacard(
        uint ownerCharacterId,
        out ActiveFriendLinkPlacard? placard
    )
    {
        lock (_friendLinkPlacardLock)
        {
            placard = null;
            if (!_friendLinkPlacardIdsByOwner.Remove(ownerCharacterId, out var placardId))
                return false;
            if (!_friendLinkPlacardsById.Remove(placardId, out placard))
                return false;
            placard.ClearComments();
            RemovePlacardCommentContexts(placardId);
            return true;
        }
    }

    public ActiveFriendLinkPlacardComment? AddFriendLinkPlacardComment(
        uint placardId,
        int authorUserId,
        uint authorCharacterId,
        string authorName,
        string message
    )
    {
        if (string.IsNullOrWhiteSpace(message) || authorCharacterId == 0)
            return null;
        lock (_friendLinkPlacardLock)
            return _friendLinkPlacardsById.TryGetValue(placardId, out var placard)
                ? placard.AddComment(authorUserId, authorCharacterId, authorName, message)
                : null;
    }

    public IReadOnlyList<IPlayerSession> GetCircleChatClients(int circleId) =>
        [
            .. GetServerClients(ServerType.Msg)
                .Where(session =>
                    session.IsAuthenticated
                    && TryGetCircleChat(session.ConnectionId, out var active)
                    && active == circleId
                ),
        ];

    public IReadOnlyList<IPlayerSession> GetOnlineMsgClientsByCharacterIds(
        IEnumerable<int> characterIds
    )
    {
        var set = characterIds.Select(id => (uint)id).ToHashSet();
        return
        [
            .. GetServerClients(ServerType.Msg)
                .Where(session => session.IsAuthenticated && set.Contains(session.CharacterId)),
        ];
    }

    public IReadOnlyList<IPlayerSession> GetOnlineMsgClientsByCharacterId(int characterId) =>
        GetOnlineMsgClientsByCharacterId((uint)characterId);

    public IReadOnlyList<IPlayerSession> GetOnlineMsgClientsByCharacterId(uint characterId) =>
        [
            .. GetServerClients(ServerType.Msg)
                .Where(session => session.IsAuthenticated && session.CharacterId == characterId),
        ];

    public void SetPendingAreaTransition(PendingMapTransfer transition)
    {
        if (_pendingMapTransferRepository == null)
            _pendingTransitionStore.SetPendingAreaTransition(transition);
        else
            _pendingMapTransferRepository.Upsert(transition, TimeSpan.FromMinutes(5));
    }

    public bool TryTakePendingAreaTransition(int userId, out PendingMapTransfer transition)
    {
        if (_pendingMapTransferRepository == null)
            return _pendingTransitionStore.TryTakePendingAreaTransition(userId, out transition);

        return _pendingMapTransferRepository.TryTake(userId, out transition);
    }

    public void EnqueueMessage(string id, string message) =>
        _messages.Writer.TryWrite((id, message));

    public ChannelReader<(string Id, string Message)> Messages => _messages.Reader;

    public IReadOnlyList<IPlayerSession> GetServerClients(ServerType serverType)
    {
        if (_sessionPresenceRepository == null)
            return [.. _sessionClientRegistry.GetClients(serverType)];

        var presences = _sessionPresenceRepository.GetByServerType(serverType);
        return ResolveConnectedSessions(presences.Select(presence => presence.ConnectionId));
    }

    public IReadOnlyList<IPlayerSession> GetAreaSessions(uint mapId, int channelId)
    {
        if (_sessionPresenceRepository == null)
            return _areaPresenceIndex.GetAreaSessions(mapId, channelId);

        var presences = _sessionPresenceRepository.GetAreaSessions(mapId, channelId);
        return ResolveConnectedSessions(presences.Select(presence => presence.ConnectionId));
    }

    public IReadOnlyList<IPlayerSession> GetAreaPeers(
        IPlayerSession session,
        bool includeSelf = false
    )
    {
        IEnumerable<IPlayerSession> peers = GetAreaSessions(session.MapId, session.ChannelId);
        if (MyRoomInfo.IsMyRoomMap(session.MapId))
            peers =
                session.MyRoomId == 0
                    ? []
                    : peers.Where(other => other.MyRoomId == session.MyRoomId);

        return
        [
            .. (
                includeSelf
                    ? peers
                    : peers.Where(other => other.ConnectionId != session.ConnectionId)
            ),
        ];
    }

    public static RoboData PrepareOwnedRobo(RoboData robo, IPlayerSession owner)
    {
        robo.OwnerAvatarId = owner.CharacterId;
        if (owner.AccompanyingRoboIds.Contains(robo.RoboId))
        {
            robo.State = (uint)RoboState.Accompanying;
            ApplyRoboMap(robo, owner);
        }
        else
        {
            robo.State = (uint)RoboState.InMyRoom;
            robo.Character.Map = new CharacterMapData();
        }

        return robo;
    }

    public static RoboData PrepareRemoteRobo(RoboData robo, IPlayerSession owner)
    {
        robo.State = (uint)RoboState.Accompanying;
        ApplyRoboMap(robo, owner);
        return robo;
    }

    public void RememberRoboMovement(uint ownerCharacterId, uint roboId, MovementData movement)
    {
        _roboLastMovement[(ownerCharacterId, roboId)] = new MovementData(
            movement.X,
            movement.Y,
            movement.Z,
            movement.Rotation,
            MovementType.Stopped
        );
    }

    public bool TryGetRoboMovement(uint ownerCharacterId, uint roboId, out MovementData movement) =>
        _roboLastMovement.TryGetValue((ownerCharacterId, roboId), out movement!);

    public async Task BroadcastAreaDisappearAsync(
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (session.CharacterId == 0)
            return;

        var peers = GetAreaPeers(session);
        TryRemoveFriendLinkPlacard(session.CharacterId, out var placard);
        if (peers.Count > 0)
        {
            if (placard is not null)
                await NotifyPlacardRemoveToPeersAsync(placard.PlacardId, peers, ct);
            await NotifyDisappearToPeersAsync(session, peers, ct);
        }
    }

    private static async Task NotifyDisappearToPeersAsync(
        IPlayerSession session,
        IReadOnlyList<IPlayerSession> peers,
        CancellationToken ct = default
    )
    {
        var payload = new NotifyDisappearChara(session.CharacterId).ToBytes();
        foreach (var peer in peers)
        {
            await peer.SendAsync(PacketType.NotifyDisappearChara, payload, ct);

            foreach (var roboId in session.AccompanyingRoboIds)
                await SendRoboDisappearAsync(peer, session.CharacterId, roboId, ct);
        }
    }

    private static async Task NotifyPlacardRemoveToPeersAsync(
        uint placardId,
        IReadOnlyList<IPlayerSession> peers,
        CancellationToken ct = default
    )
    {
        var payload = new NotifyPlacardRemove(placardId).ToBytes();
        foreach (var peer in peers)
            await peer.SendAsync(PacketType.NotifyPlacardRemove, payload, ct);
    }

    public async Task BroadcastRoboDisappearAsync(
        IPlayerSession owner,
        uint roboId,
        CancellationToken ct = default
    )
    {
        foreach (var peer in GetAreaPeers(owner))
            await SendRoboDisappearAsync(peer, owner.CharacterId, roboId, ct);
    }

    public async Task ClearRemoteRobosAsync(IPlayerSession session, CancellationToken ct = default)
    {
        foreach (var remoteRoboObjectId in session.VisibleRemoteRoboObjectIds.ToArray())
            await session.SendAsync(
                PacketType.NotifyDisappearChara,
                new NotifyDisappearChara(remoteRoboObjectId).ToBytes(),
                ct
            );

        session.VisibleRemoteRoboObjectIds.Clear();
    }

    public IPlayerSession? GetAreaSessionByCharacterId(
        uint characterId,
        uint? mapId = null,
        int? channelId = null
    )
    {
        if (_sessionPresenceRepository == null)
            return _areaPresenceIndex.GetAreaSessionByCharacterId(characterId, mapId, channelId);

        var presence = _sessionPresenceRepository.GetAreaSessionByCharacterId(
            characterId,
            mapId,
            channelId
        );
        if (presence == null)
            return null;

        _sessionStore.TryGetSession(presence.ConnectionId, out var session);
        return session;
    }

    private static async Task SendRoboDisappearAsync(
        IPlayerSession peer,
        uint ownerCharacterId,
        uint roboId,
        CancellationToken ct
    )
    {
        var remoteRoboObjectId = RoboRepository.GetObjectId(ownerCharacterId, roboId);
        if (!peer.VisibleRemoteRoboObjectIds.Remove(remoteRoboObjectId))
            return;

        await peer.SendAsync(
            PacketType.NotifyDisappearChara,
            new NotifyDisappearChara(remoteRoboObjectId).ToBytes(),
            ct
        );
    }

    private static void ApplyRoboMap(RoboData robo, IPlayerSession owner)
    {
        robo.OwnerAvatarId = owner.CharacterId;
        robo.Character.Map = new CharacterMapData
        {
            ChannelId = checked((uint)owner.ChannelId),
            MapId = owner.MapId,
            Movement = new MovementData(
                owner.X,
                owner.Y,
                owner.Z,
                owner.Rotation,
                MovementType.Stopped
            ),
        };
    }

    public IPlayerSession? GetAreaSessionByUserId(
        int userId,
        uint? mapId = null,
        int? channelId = null
    )
    {
        if (_sessionPresenceRepository == null)
            return _areaPresenceIndex.GetAreaSessionByUserId(userId, mapId, channelId);

        var presence = _sessionPresenceRepository.GetAreaSessionByUserId(userId, mapId, channelId);
        if (presence == null)
            return null;

        _sessionStore.TryGetSession(presence.ConnectionId, out var session);
        return session;
    }

    private void CloseSupersededClients(ServerType serverType, IPlayerSession session)
    {
        foreach (var existing in GetServerClients(serverType))
        {
            if (existing.ConnectionId == session.ConnectionId)
                continue;

            if (!ShouldSupersede(serverType, existing, session))
                continue;

            CloseAndUnregister(serverType, existing);
        }
    }

    private void CloseAndUnregister(ServerType serverType, IPlayerSession existing)
    {
        IReadOnlyList<IPlayerSession>? disappearPeers = null;
        if (serverType == ServerType.Area && existing.CharacterId != 0)
            disappearPeers = GetAreaPeers(existing);

        UnregisterClient(serverType, existing.ConnectionId);
        if (existing is PlayerSession playerSession)
            playerSession.ClientConnection.Dispose();

        if (disappearPeers is { Count: > 0 })
            _ = NotifyDisappearToPeersAsync(existing, disappearPeers);
    }

    private static bool BelongsToUser(IPlayerSession session, int userId) =>
        userId > 0
        && (
            session.UserId == userId
            || session.User?.Id == userId
            || session.Character?.UserId == userId
        );

    private static bool ShouldSupersede(
        ServerType serverType,
        IPlayerSession existing,
        IPlayerSession incoming
    ) =>
        serverType switch
        {
            ServerType.Area => existing.CharacterId != 0
                && existing.CharacterId == incoming.CharacterId,
            ServerType.Auth or ServerType.Msg => existing.UserId != 0
                && existing.UserId == incoming.UserId,
            _ => false,
        };

    private IReadOnlyList<IPlayerSession> ResolveConnectedSessions(IEnumerable<Guid> connectionIds)
    {
        var sessions = new List<IPlayerSession>();
        foreach (var connectionId in connectionIds)
        {
            if (_sessionStore.TryGetSession(connectionId, out var session) && session != null)
                sessions.Add(session);
        }

        return sessions;
    }

    private sealed record ActivePlacardComment(uint PlacardId, DateTime ExpiresAtUtc);

    private void RemovePlacardCommentContexts(uint? placardId)
    {
        if (placardId is null)
            return;
        foreach (var context in _activePlacardComments)
        {
            if (context.Value.PlacardId == placardId.Value)
                _activePlacardComments.TryRemove(context.Key, out _);
        }
    }

    public readonly record struct PendingMapTransfer(
        int UserId,
        uint MapId,
        int ChannelId,
        float X,
        float Y,
        float Z,
        int Rotation,
        uint MyRoomId = 0
    );
}
