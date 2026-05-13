using System.Text;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Common;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Msg;

public class LoginHandler(IUserSessionRepository sessionRepo, SharedState state, ILogger<LoginHandler> logger) : PacketHandlerBase<LoginRequest, LoginResponse>
{
    public override PacketType RequestType => PacketType.LoginRequest;
    public override PacketType ResponseType => PacketType.LoginResponse;
    public override ServerType ServerType => ServerType.Msg;

    private readonly IUserSessionRepository _sessionRepo = sessionRepo;
    private readonly ILogger<LoginHandler> _logger = logger;

    public override async Task<LoginResponse?> HandleAsync(LoginRequest request, IPlayerSession session, CancellationToken ct = default)
    {
        var otp = Encoding.ASCII.GetString(request._otp);
        _logger.LogInformation("ListenerId: {ListenId} LoginRequest UserID: {UserID}, OTP: {OTP}", session.ConnectionId, request._userId, otp);

        var userSession = await _sessionRepo.GetValidSessionAsync(otp, ct);
        if (userSession is null || userSession.UserId != request._userId)
        {
            _logger.LogWarning("Client: {ClientId} Login failed for UserID: {UserID} with OTP: {OTP}", session.ConnectionId, request._userId, otp);
            return new LoginResponse(AuthResponseResult.InvalidCredentials);
        }

        if (userSession.User.IsBanned)
        {
            _logger.LogWarning("Client: {ClientId} Login rejected: user {UserID} is banned", session.ConnectionId, request._userId);
            return new LoginResponse(AuthResponseResult.AccountBanned);
        }

        session.User = userSession.User;
        session.UserId = userSession.User.Id;
        state.RegisterClient(ServerType.Msg, session);
        _logger.LogInformation("Client: {ClientId} LoginRequest UserID: {UserID}, OTP: {OTP}, Name: {name}", session.ConnectionId, request._userId, otp, session.User!.Username);
        return new LoginResponse(AuthResponseResult.Success);
    }
}
