using System.Collections.Concurrent;
using AISpace.Network;

namespace AISpace.Common.Game;

public class SharedState
{
    public ConcurrentDictionary<Guid, ClientConnection> AuthClients = new();
    public ConcurrentDictionary<Guid, ClientConnection> MsgClients = new();
    public ConcurrentDictionary<Guid, ClientConnection> AreaClients = new();
    public ConcurrentQueue<(string id, string message)> newMessages = new();
    public readonly long StartTimeUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public void RegisterClient(string serverName, ClientConnection client)
    {
        if (serverName == "Area")
        {
            var ghost = AreaClients.Values.FirstOrDefault(c => c.CharacterId == client.CharacterId);
            if (ghost != null && ghost.Id != client.Id)
            {
                AreaClients.TryRemove(ghost.Id, out _);
            }
            AreaClients[client.Id] = client;
        }
        else if (serverName == "Msg")
        {
            MsgClients[client.Id] = client;
        }
    }

    public void UnregisterClient(string serverName, Guid clientId)
    {
        AuthClients.TryRemove(clientId, out _);
        MsgClients.TryRemove(clientId, out _);
        AreaClients.TryRemove(clientId, out _);
    }
}
