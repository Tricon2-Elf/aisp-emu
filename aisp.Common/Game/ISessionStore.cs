namespace aisp.Common.Game;

public interface ISessionStore
{
    IPlayerSession GetOrAddSession(Guid connectionId, Func<IPlayerSession> createSession);
    bool TryGetSession(Guid connectionId, out IPlayerSession? session);
    IReadOnlyList<IPlayerSession> GetSessions();

    bool RemoveSession(Guid connectionId);
}
