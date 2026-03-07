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
    public IPlayerSession GetOrAddSession(Guid connectionId, Func<IPlayerSession> createSession) =>
        _sessionByConnectionId.GetOrAdd(connectionId, _ => createSession());

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

    public IPlayerSession? GetAreaSessionByCharacterId(uint characterId) =>
        AreaClients.Values.FirstOrDefault(s => s.CharacterId == characterId);
}
