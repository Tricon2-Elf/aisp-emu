using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Network;
using AISpace.Common.Network.Packets.Area;
using AISpace.Common.Network.Packets.Common;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers;

public class AreasvEnterHandler(ILogger<AreasvEnterHandler> _logger, IUserSessionRepository _sessionRepo, SharedState state) : IPacketHandler
{
    public PacketType RequestType => PacketType.AreasvEnterRequest;

    public PacketType ResponseType => PacketType.AreasvEnterResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var loginReq = AreasvEnterRequest.FromBytes(payload.Span);
        _logger.LogInformation("Client: {Id} EnterRequest UserID: {UserID}, SessionID: {OTP}", connection.Id, loginReq.UserID, loginReq.OTP);
        var session = await _sessionRepo.GetValidSessionAsync(loginReq.OTP, ct);

        if (session is null || session.UserId != loginReq.UserID)
        {
            _logger.LogWarning("Client: {ClientId} Login failed for UserID: {UserID} with OTP: {OTP}", connection.Id, loginReq.UserID, loginReq.OTP);
            await connection.SendAsync(ResponseType, new LoginResponse(AuthResponseResult.InvalidCredentials).ToBytes(), ct);
            return;
        }

        connection.User = session.User;
        var charId = (uint)connection.User.Characters.First().Id;
        _logger.LogInformation("Client: {ClientId} LoginRequest UserID: {UserID}, OTP: {OTP}, Name: {name}, CharID {charid}, CharName: {cname}", connection.Id, loginReq.UserID, loginReq.OTP, connection.User.Username, charId, connection.User.Characters.First().Name);

        state.RegisterClient("Area", connection);

        var response = new AreasvEnterResponse(0, charId);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
