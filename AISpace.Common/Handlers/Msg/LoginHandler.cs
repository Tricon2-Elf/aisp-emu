using System.Text;
using AISpace.Network;
using AISpace.Network.Packets.Common;

namespace AISpace.Common.Handlers.Msg;

public class LoginHandler(IUserSessionRepository sessionRepo, ILogger<LoginHandler> logger) : PacketHandlerBase<LoginRequest, LoginResponse>
{
    public override PacketType RequestType => PacketType.LoginRequest;
    public override PacketType ResponseType => PacketType.LoginResponse;
    public override MessageDomain Domain => MessageDomain.Msg;

    private readonly IUserSessionRepository _sessionRepo = sessionRepo;
    private readonly ILogger<LoginHandler> _logger = logger;

    public override async Task<LoginResponse?> HandleAsync(LoginRequest request, ClientConnection connection, CancellationToken ct = default)
    {
        var otp = Encoding.ASCII.GetString(request._otp);
        _logger.LogInformation("ListenerId: {ListenId} LoginRequest UserID: {UserID}, OTP: {OTP}", connection.Id, request._userId, otp);

        var session = await _sessionRepo.GetValidSessionAsync(otp, ct);
        if (session is null || session.UserId != request._userId)
        {
            _logger.LogWarning("Client: {ClientId} Login failed for UserID: {UserID} with OTP: {OTP}", connection.Id, request._userId, otp);
            return new LoginResponse(AuthResponseResult.InvalidCredentials);
        }

        connection.User = session.User;
        _logger.LogInformation("Client: {ClientId} LoginRequest UserID: {UserID}, OTP: {OTP}, Name: {name}", connection.Id, request._userId, otp, connection.User.Username);
        return new LoginResponse(AuthResponseResult.Success);
    }
}
