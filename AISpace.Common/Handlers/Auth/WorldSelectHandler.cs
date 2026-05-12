using AISpace.Common.Config;
using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Crypto;
using AISpace.Network.Packets.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AISpace.Common.Handlers.Auth;

public class WorldSelectHandler(IWorldRepository worldRepo, IUserSessionRepository sessionRepo, ILogger<WorldSelectHandler> logger, IOptions<ServerOptions> serverOptions) : IPacketHandler
{
    private readonly IWorldRepository _worldRepository = worldRepo;
    private readonly IUserSessionRepository _sessionRepo = sessionRepo;
    private readonly ILogger<WorldSelectHandler> _logger = logger;

    public PacketType RequestType => PacketType.WorldSelectRequest;
    public PacketType ResponseType => PacketType.WorldSelectResponse;
    public ServerType ServerType => ServerType.Auth;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var WorldSelectReq = WorldSelectRequest.FromBytes(payload.Span);
        var selectedWorldID = (int)WorldSelectReq.WorldID;

        if (!session.IsAuthenticated)
        {
            _logger.LogWarning("WorldSelectRequest rejected: session not authenticated (client may have sent WorldSelect before Authenticate). Sending error response.");
            var errResp = new WorldSelectResponse(1, "", 0, "");
            await session.SendAsync(PacketType.WorldSelectResponse, errResp.ToBytes(), ct);
            return;
        }

        if (session.User!.IsBanned)
        {
            _logger.LogWarning("WorldSelectRequest rejected: user {Username} is banned", session.User!.Username);
            var errResp = new WorldSelectResponse(1, "", 0, "");
            await session.SendAsync(PacketType.WorldSelectResponse, errResp.ToBytes(), ct);
            return;
        }

        var world = await _worldRepository.GetByIdAsync(selectedWorldID);
        if (world == null)
        {
            _logger.LogWarning("WorldSelectRequest: world {WorldId} not found. Sending error response.", selectedWorldID);
            var errResp = new WorldSelectResponse(1, "", 0, "");
            await session.SendAsync(PacketType.WorldSelectResponse, errResp.ToBytes(), ct);
            return;
        }

        User clientUser = session.User!;
        string otp = CryptoUtils.GenerateOTP();
        await _sessionRepo.CreateAsync(clientUser.Id, otp, TimeSpan.FromMinutes(5), ct);
        _logger.LogInformation("World Selected: {ID}", selectedWorldID);
        var resolvedAddress = serverOptions.Value.ResolveAddress(world.Address);
        var WorldSelectResp = new WorldSelectResponse(0, resolvedAddress, world.Port, otp);
        await session.SendAsync(PacketType.WorldSelectResponse, WorldSelectResp.ToBytes(), ct);
    }
}
