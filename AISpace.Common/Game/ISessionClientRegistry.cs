using System.Collections.Concurrent;

namespace AISpace.Common.Game;

public interface ISessionClientRegistry
{
    void Register(ServerType serverType, IPlayerSession session);

    void Unregister(Guid connectionId);

    IReadOnlyCollection<IPlayerSession> GetClients(ServerType serverType);

    ConcurrentDictionary<Guid, IPlayerSession> GetClientMap(ServerType serverType);
}
