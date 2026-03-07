using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Auth;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Auth;

public class AuthenticateHandler(IUserRepository userRepo, ILogger<AuthenticateHandler> logger) : PacketHandlerBase<AuthenticateRequest, AuthenticateResponse>
{
    private readonly ILogger<AuthenticateHandler> _logger = logger;

    public override PacketType RequestType => PacketType.AuthenticateRequest;
    public override PacketType ResponseType => PacketType.AuthenticateResponse;
    public override MessageDomain Domain => MessageDomain.Auth;

    public override async Task<AuthenticateResponse?> HandleAsync(AuthenticateRequest request, IPlayerSession session, CancellationToken ct = default)
    {
        _logger.LogInformation($"Auth request: {request.Username}");

        var user = await userRepo.GetByUsernameAsync(request.Username);

        if (user == null)
        {
            _logger.LogInformation($"User '{request.Username}' not found. Creating new account...");
            await userRepo.AddAsync(request.Username, request.Password);

            user = await userRepo.GetByUsernameAsync(request.Username);
        }
        else
        {
            if (!user.VerifyPassword(request.Password))
            {
                _logger.LogWarning($"Auth failed: Wrong password for user '{request.Username}'");
                var failResp = new AuthenticateFailureResponse(AuthResponseResult.InvalidCredentials);
                await session.SendAsync(PacketType.AuthenticateFailureResponse, failResp.ToBytes(), ct);
                return null;
            }
        }

        if (user == null)
            return null;

        _logger.LogInformation($"User '{user.Username}' (ID: {user.Id}) logged in successfully.");
        session.User = user;
        session.UserId = user.Id;
        return new AuthenticateResponse((uint)user.Id);
    }
}
