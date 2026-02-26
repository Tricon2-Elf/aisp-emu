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

    // Method to broadcast to all clients in the Area world
    public static async Task BroadcastAreaAsync(PacketType type, byte[] data, Guid excludeId, CancellationToken ct = default)
    {
        foreach (var session in _sessions.Values)
        {
            // Send only to authenticated clients in the 3D world and not to oneself
            if (session.Id != excludeId && session.IsAuthenticated && session.CharacterId != 0)
            {
                await session.SendAsync(type, data, ct);
            }
        }
    }
}
