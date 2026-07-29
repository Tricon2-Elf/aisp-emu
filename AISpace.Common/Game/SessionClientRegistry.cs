using System.Collections.Concurrent;

namespace AISpace.Common.Game;

public sealed class SessionClientRegistry : ISessionClientRegistry
{
    private readonly ConcurrentDictionary<Guid, IPlayerSession> _authClients = new();
    private readonly ConcurrentDictionary<Guid, IPlayerSession> _msgClients = new();
    private readonly ConcurrentDictionary<Guid, IPlayerSession> _areaClients = new();

    public void Register(ServerType serverType, IPlayerSession session)
    {
        switch (serverType)
        {
            case ServerType.Area:
                var ghost = _areaClients.Values.FirstOrDefault(existing =>
                    existing.CharacterId == session.CharacterId
                );
                if (ghost != null && ghost.ConnectionId != session.ConnectionId)
                    _areaClients.TryRemove(ghost.ConnectionId, out _);

                _areaClients[session.ConnectionId] = session;
                break;
            case ServerType.Msg:
                _msgClients[session.ConnectionId] = session;
                break;
            case ServerType.Auth:
                _authClients[session.ConnectionId] = session;
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(serverType),
                    serverType,
                    "Unsupported server type."
                );
        }
    }

    public void Unregister(Guid connectionId)
    {
        _authClients.TryRemove(connectionId, out _);
        _msgClients.TryRemove(connectionId, out _);
        _areaClients.TryRemove(connectionId, out _);
    }

    public IReadOnlyCollection<IPlayerSession> GetClients(ServerType serverType)
    {
        return GetClientMap(serverType).Values.ToList();
    }

    public ConcurrentDictionary<Guid, IPlayerSession> GetClientMap(ServerType serverType)
    {
        return serverType switch
        {
            ServerType.Auth => _authClients,
            ServerType.Area => _areaClients,
            ServerType.Msg => _msgClients,
            _ => throw new ArgumentOutOfRangeException(
                nameof(serverType),
                serverType,
                "Unsupported server type."
            ),
        };
    }
}
