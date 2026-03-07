using AISpace.Common.Network.Packets.Auth;
using AISpace.Network;
using AISpace.Network.Crypto;
using AISpace.Network.Packets.Auth;

namespace AISpace.Common.Handlers.Auth;

public class WorldSelectHandler(IWorldRepository worldRepo, IUserSessionRepository sessionRepo, ILogger<WorldSelectHandler> logger, IOptions<ServerOptions> serverOptions) : IPacketHandler
{
    private readonly IWorldRepository _worldRepository = worldRepo;
    private readonly IUserSessionRepository _sessionRepo = sessionRepo;
    private readonly ILogger<WorldSelectHandler> _logger = logger;

    public PacketType RequestType => PacketType.Auth_WorldSelectRequest;
    public PacketType ResponseType => PacketType.Auth_WorldSelectResponse;
    public MessageDomain Domain => MessageDomain.Auth;

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
        var resolvedAddress = serverOptions.Value.ResolveAddress(world.Address);
        var WorldSelectResp = new WorldSelectResponse(0, resolvedAddress, world.Port, otp);
        await connection.SendAsync(PacketType.Auth_WorldSelectResponse, WorldSelectResp, ct);
    }
}
