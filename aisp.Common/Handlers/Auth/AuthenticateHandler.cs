using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Auth;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Auth;

public class AuthenticateHandler(
    IUserRepository userRepo,
    SharedState state,
    ILogger<AuthenticateHandler> logger
) : PacketHandlerBase<AuthenticateRequest, AuthenticateResponse>
{
    private readonly ILogger<AuthenticateHandler> _logger = logger;

    public override PacketType RequestType => PacketType.AuthenticateRequest;
    public override PacketType ResponseType => PacketType.AuthenticateResponse;
    public override ServerType ServerType => ServerType.Auth;

    public override async Task<AuthenticateResponse?> HandleAsync(
        AuthenticateRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        _logger.LogInformation("Auth request: {Username}", request.Username);

        var user = await userRepo.GetByUsernameAsync(request.Username);

        if (user == null || !user.VerifyPassword(request.Password))
        {
            if (user == null)
            {
                _logger.LogWarning(
                    "Auth failed: Unknown user '{Username}' (accounts must be created via the web portal)",
                    request.Username
                );
            }
            else
            {
                _logger.LogWarning(
                    "Auth failed: Wrong password for user '{Username}'",
                    request.Username
                );
            }

            var failResp = new AuthenticateFailureResponse(AuthResponseResult.InvalidCredentials);
            await session.SendAsync(PacketType.AuthenticateFailureResponse, failResp.ToBytes(), ct);
            return null;
        }

        if (user.IsBanned)
        {
            _logger.LogWarning(
                $"Auth rejected: User '{user.Username}' is banned. Reason: {user.BanReason}"
            );
            var banResp = new AuthenticateFailureResponse(AuthResponseResult.AccountBanned);
            await session.SendAsync(PacketType.AuthenticateFailureResponse, banResp.ToBytes(), ct);
            return null;
        }

        await userRepo.TouchLastLoggedInAsync(user.Id, ct);
        user.LastLoggedInAt = DateTime.UtcNow;
        _logger.LogInformation($"User '{user.Username}' (ID: {user.Id}) logged in successfully.");
        session.User = user;
        session.UserId = user.Id;
        state.RegisterClient(ServerType.Auth, session);
        return new AuthenticateResponse((uint)user.Id);
    }
}
