using AISpace.Common.Network.Packets.Auth;
using AISpace.Network;

namespace AISpace.Common.Handlers.Auth;

public class WorldListHandler(IWorldRepository repo, ILogger<WorldListHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.Auth_WorldListRequest;
    public PacketType ResponseType => PacketType.Auth_WorldListResponse;
    public MessageDomain Domain => MessageDomain.Auth;

    private readonly IWorldRepository _worldRepository = repo;
    private readonly ILogger<WorldListHandler> _logger = logger;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        try
        {
            var worlds = await _worldRepository.GetAllAsync();
            var worldListResponse = new WorldListResponse(0, worlds);

            await connection.SendAsync(PacketType.Auth_WorldListResponse, worldListResponse.ToBytes(), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("{Message} | {all}", ex.Message, ex.ToString());
        }
    }
}
