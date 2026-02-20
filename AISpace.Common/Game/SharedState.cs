using System.Collections.Concurrent;
using AISpace.Common.Network;

namespace AISpace.Common.Game;

public class SharedState
{
    public ConcurrentQueue<(string id, string message)> newMessages = new();

    public ConcurrentQueue<(string id, MovementData moveData)> newMovement = new();

    public ConcurrentDictionary<Guid, ClientConnection> MsgClients = new();
    public ConcurrentDictionary<Guid, ClientConnection> AreaClients = new();

    public void RegisterClient(string serverName, ClientConnection client)
    {
        if (serverName == "Msg")
            MsgClients.TryAdd(client.Id, client);
        else if (serverName == "Area")
            AreaClients.TryAdd(client.Id, client);
    }

    public void UnregisterClient(string serverName, Guid clientId)
    {
        MsgClients.TryRemove(clientId, out _);
        AreaClients.TryRemove(clientId, out _);
    }
}
