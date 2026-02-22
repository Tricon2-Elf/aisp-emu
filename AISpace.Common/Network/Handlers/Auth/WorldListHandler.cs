using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Network.Packets.Auth;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers.Auth;

public class WorldListHandler(IWorldRepository repo, ILogger<WorldListHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.Auth_WorldListRequest;
    public PacketType ResponseType => PacketType.Auth_WorldListResponse;
    public MessageDomain Domain => MessageDomain.Auth;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var dbWorlds = await repo.GetAllAsync();
        var patchedWorlds = new List<World>();
        
        string hardcodedIp = "192.168.31.158";

        foreach (var w in dbWorlds)
        {
            patchedWorlds.Add(new World 
            { 
                Id = w.Id, 
                Name = w.Name, 
                Description = w.Description + " (Multiplayer)", 
                Port = w.Port,
                Address = hardcodedIp
            });
        }

        var response = new WorldListResponse(0, patchedWorlds);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}