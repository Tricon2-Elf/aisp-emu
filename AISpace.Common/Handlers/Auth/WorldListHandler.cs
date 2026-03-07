using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Auth;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Auth;

public class WorldListHandler(IWorldRepository repo, ILogger<WorldListHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.Auth_WorldListRequest;
    public PacketType ResponseType => PacketType.Auth_WorldListResponse;
    public MessageDomain Domain => MessageDomain.Auth;

    private readonly IWorldRepository _worldRepository = repo;
    private readonly ILogger<WorldListHandler> _logger = logger;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        try
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

            await session.SendAsync(PacketType.Auth_WorldListResponse, worldListResponse.ToBytes(), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("{Message} | {all}", ex.Message, ex.ToString());
        }
    }
}
