using System.Collections.Concurrent;
using AISpace.Common.Network;
using AISpace.Common.Network.Packets;

namespace AISpace.Common.Game;

public static class WorldSessionManager
{
    private static readonly ConcurrentDictionary<Guid, ClientConnection> _sessions = new();

    public static void AddSession(ClientConnection connection)
    {
        _sessions[connection.Id] = connection;
    }

    public static void RemoveSession(Guid id)
    {
        _sessions.TryRemove(id, out _);
    }

    public static IEnumerable<ClientConnection> GetAllSessions() => _sessions.Values;

    // Метод для рассылки всем, кто в мире Area
    public static async Task BroadcastAreaAsync(PacketType type, byte[] data, Guid excludeId, CancellationToken ct = default)
    {
        foreach (var session in _sessions.Values)
        {
            // Шлем только тем, кто залогинен и находится в 3D мире, и не самому себе
            if (session.Id != excludeId && session.IsAuthenticated && session.CharacterId != 0)
            {
                await session.SendAsync(type, data, ct);
            }
        }
    }
}