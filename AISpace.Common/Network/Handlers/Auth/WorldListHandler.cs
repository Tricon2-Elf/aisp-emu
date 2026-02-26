using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Network.Packets.Auth;

namespace AISpace.Common.Network.Handlers.Auth;

public class WorldListHandler(IWorldRepository repo) : IPacketHandler
{
    public PacketType RequestType => PacketType.Auth_WorldListRequest;
    public PacketType ResponseType => PacketType.Auth_WorldListResponse;
    public MessageDomain Domain => MessageDomain.Auth;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var dbWorlds = await repo.GetAllAsync();
        var patchedWorlds = new List<World>();

        var response = new WorldListResponse(0, dbWorlds);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
