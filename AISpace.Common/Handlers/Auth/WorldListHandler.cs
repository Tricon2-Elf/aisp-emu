using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Auth;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Auth;

public class WorldListHandler(IWorldRepository repo, ILogger<WorldListHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.WorldListRequest;
    public PacketType ResponseType => PacketType.WorldListResponse;
    public ServerType ServerType => ServerType.Auth;

    private readonly IWorldRepository _worldRepository = repo;
    private readonly ILogger<WorldListHandler> _logger = logger;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var worlds = await _worldRepository.GetAllAsync();
        var worldDataList = worlds
            .Select(w => new WorldData
            {
                Id = w.Id,
                Name = w.Name,
                Description = w.Description,
                Address = w.Address,
                Port = w.Port,
            })
            .ToList();
        var worldListResponse = new WorldListResponse(0, worldDataList);

        await session.SendAsync(PacketType.WorldListResponse, worldListResponse.ToBytes(), ct);
    }
}
