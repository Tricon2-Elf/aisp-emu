using System.Text;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Common;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Msg;

public class LoginHandler(
    IUserSessionRepository sessionRepo,
    IUserRepository userRepo,
    ICircleRepository circles,
    SharedState state,
    ILogger<LoginHandler> logger
) : PacketHandlerBase<LoginRequest, LoginResponse>
{
    public override PacketType RequestType => PacketType.LoginRequest;
    public override PacketType ResponseType => PacketType.LoginResponse;
    public override ServerType ServerType => ServerType.Msg;

    private readonly IUserSessionRepository _sessionRepo = sessionRepo;
    private readonly ILogger<LoginHandler> _logger = logger;

    public override async Task<LoginResponse?> HandleAsync(
        LoginRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var otp = Encoding.ASCII.GetString(request._otp);
        _logger.LogInformation(
            "ListenerId: {ListenId} LoginRequest UserID: {UserID}, OTP: {OTP}",
            session.ConnectionId,
            request._userId,
            otp
        );

        var userSession = await _sessionRepo.GetValidSessionAsync(otp, ct);
        if (userSession is null || userSession.UserId != request._userId)
        {
            _logger.LogWarning(
                "Client: {ClientId} Login failed for UserID: {UserID} with OTP: {OTP}",
                session.ConnectionId,
                request._userId,
                otp
            );
            return new LoginResponse(AuthResponseResult.InvalidCredentials);
        }

        var user =
            await UserModerationState.PrepareUserForGameLoginAsync(
                userRepo,
                userSession.UserId,
                ct
            ) ?? userSession.User;

        if (UserModerationState.IsCurrentlyBanned(user))
        {
            _logger.LogWarning(
                "Client: {ClientId} Login rejected: user {UserID} is banned",
                session.ConnectionId,
                request._userId
            );
            return new LoginResponse(AuthResponseResult.AccountBanned);
        }

        if (UserModerationState.IsCurrentlyKicked(user))
        {
            _logger.LogWarning(
                "Client: {ClientId} Login rejected: user {UserID} is kicked until {KickedUntil}",
                session.ConnectionId,
                request._userId,
                user.KickedUntil
            );
            return new LoginResponse(AuthResponseResult.Failure);
        }

        session.User = user;
        session.UserId = user.Id;
        session.Language = user.Language;

        var character = user.Characters.OrderBy(c => c.Id).FirstOrDefault();
        if (character != null)
        {
            session.CharacterId = (uint)character.Id;
            session.Character = character;
        }

        state.DisconnectOtherConnectionsForUser(session.UserId, session.ConnectionId);
        state.RegisterClient(ServerType.Msg, session);
        if (session.CharacterId != 0)
        {
            try
            {
                await CircleNotifyHelper.NotifyMemberLoginAsync(
                    circles,
                    state,
                    (int)session.CharacterId,
                    ct
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed broadcasting circle login for character {CharacterId}",
                    session.CharacterId
                );
            }
        }

        _logger.LogInformation(
            "Client: {ClientId} LoginRequest UserID: {UserID}, OTP: {OTP}, Name: {name}",
            session.ConnectionId,
            request._userId,
            otp,
            session.Character?.Name ?? session.User!.Username
        );
        return new LoginResponse(AuthResponseResult.Success);
    }
}
