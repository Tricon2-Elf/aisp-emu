using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Network.Crypto;
using AISpace.Common.Network.Packets.Auth;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers.Auth;

public class WorldSelectHandler(IWorldRepository worldRepo, IUserSessionRepository sessionRepo, ILogger<WorldSelectHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.Auth_WorldSelectRequest;
    public PacketType ResponseType => PacketType.Auth_WorldSelectResponse;
    public MessageDomain Domain => MessageDomain.Auth;

    private readonly IWorldRepository _worldRepository = worldRepo;
    private readonly IUserSessionRepository _sessionRepo = sessionRepo;
    private readonly ILogger<WorldSelectHandler> _logger = logger;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var WorldSelectReq = WorldSelectRequest.FromBytes(payload.Span);
        var selectedWorldID = (int)WorldSelectReq.WorldID;
        var world = await _worldRepository.GetByIdAsync(selectedWorldID);
        if (world == null) //TODO: Should send a Logout notification?
            return;
        if (!connection.IsAuthenticated) //TODO: Should send a Logout notification?
            return;

        User clientUser = connection.User!;

        string otp = CryptoUtils.GenerateOTP();
        //Need to insert the otp into UserSessions
        await _sessionRepo.CreateAsync(clientUser.Id, otp, TimeSpan.FromHours(1), ct);
        _logger.LogInformation("World Selected: {ID}", selectedWorldID);
        var WorldSelectResp = new WorldSelectResponse(0, world.Address, world.Port, otp);
        await connection.SendAsync(PacketType.Auth_WorldSelectResponse, WorldSelectResp, ct);
    }
}
