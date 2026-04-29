using System.Collections.Concurrent;

namespace AISpace.Common.Game;

public sealed class SessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<Guid, IPlayerSession> _sessionByConnectionId = new();

    public IPlayerSession GetOrAddSession(Guid connectionId, Func<IPlayerSession> createSession) => _sessionByConnectionId.GetOrAdd(connectionId, _ => createSession());

    public bool TryGetSession(Guid connectionId, out IPlayerSession? session) => _sessionByConnectionId.TryGetValue(connectionId, out session);

    public IReadOnlyList<IPlayerSession> GetSessions() => _sessionByConnectionId.Values.ToList();

    public bool RemoveSession(Guid connectionId) => _sessionByConnectionId.TryRemove(connectionId, out _);
}
