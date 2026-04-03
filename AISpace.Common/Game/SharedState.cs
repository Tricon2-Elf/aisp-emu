using System.Collections.Concurrent;

namespace AISpace.Common.Game;

public class SharedState
{
    private readonly ConcurrentDictionary<Guid, IPlayerSession> _sessionByConnectionId = new();

    public ConcurrentDictionary<Guid, IPlayerSession> AuthClients = new();
    public ConcurrentDictionary<Guid, IPlayerSession> MsgClients = new();
    public ConcurrentDictionary<Guid, IPlayerSession> AreaClients = new();
    public ConcurrentQueue<(string id, string message)> newMessages = new();
    public readonly long StartTimeUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    /// <summary>Gets or creates a session for the given connection id. Factory is invoked only when a new session is needed.</summary>
    public IPlayerSession GetOrAddSession(Guid connectionId, Func<IPlayerSession> createSession) => _sessionByConnectionId.GetOrAdd(connectionId, _ => createSession());

    public void RegisterClient(string serverName, IPlayerSession session)
    {
        if (serverName == "Area")
        {
            var ghost = AreaClients.Values.FirstOrDefault(s => s.CharacterId == session.CharacterId);
            if (ghost != null && ghost.ConnectionId != session.ConnectionId)
            {
                AreaClients.TryRemove(ghost.ConnectionId, out _);
            }
            AreaClients[session.ConnectionId] = session;
        }
        else if (serverName == "Msg")
        {
            MsgClients[session.ConnectionId] = session;
        }
    }

    public void UnregisterClient(string serverName, Guid clientId)
    {
        AuthClients.TryRemove(clientId, out _);
        MsgClients.TryRemove(clientId, out _);
        AreaClients.TryRemove(clientId, out _);
        _sessionByConnectionId.TryRemove(clientId, out _);
    }

    public IReadOnlyList<IPlayerSession> GetAreaSessions(uint mapId, int channelId)
    {
        return AreaClients.Values.Where(session => IsInArea(session, mapId, channelId)).ToList();
    }

    public IReadOnlyList<IPlayerSession> GetAreaPeers(IPlayerSession session, bool includeSelf = false)
    {
        var peers = GetAreaSessions(session.MapId, session.ChannelId);

        return includeSelf ? peers : peers.Where(other => other.ConnectionId != session.ConnectionId).ToList();
    }

    public IPlayerSession? GetAreaSessionByCharacterId(uint characterId, uint? mapId = null, int? channelId = null)
    {
        IEnumerable<IPlayerSession> candidates = AreaClients.Values.Where(session => session.CharacterId == characterId);

        if (mapId.HasValue)
            candidates = candidates.Where(session => IsInArea(session, mapId.Value, channelId ?? 0));

        return candidates.FirstOrDefault();
    }

    private static bool IsInArea(IPlayerSession session, uint mapId, int channelId)
    {
        if (session.MapId != mapId)
            return false;

        if (channelId == 0 || session.ChannelId == 0)
            return true;

        return session.ChannelId == channelId;
    }
}
