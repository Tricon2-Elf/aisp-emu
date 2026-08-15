using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Localisation;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Auth;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Auth;

public class WorldListHandler(
    IWorldRepository repo,
    ITextLocaliser localiser,
    ILogger<WorldListHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.WorldListRequest;
    public PacketType ResponseType => PacketType.WorldListResponse;
    public ServerType ServerType => ServerType.Auth;

    private readonly IWorldRepository _worldRepository = repo;
    private readonly ILogger<WorldListHandler> _logger = logger;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var worlds = await _worldRepository.GetAllAsync();
        var worldDataList = worlds
            .Select(w => new WorldData
            {
                Id = w.Id,
                Name = localiser.Get(session, L.World.Name(w.Name)),
                Description = localiser.Get(session, L.World.Description(w.Name)),
                Address = w.Address,
                Port = w.Port,
            })
            .ToList();
        var worldListResponse = new WorldListResponse(0, worldDataList);

        await session.SendAsync(PacketType.WorldListResponse, worldListResponse.ToBytes(), ct);
    }
}
