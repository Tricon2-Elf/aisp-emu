using AISpace.Common.DAL.Repositories;
using AISpace.Common.Network.Packets.Common;
using Microsoft.Extensions.Logging; // Обязательно для ILogger

namespace AISpace.Common.Network.Handlers.Msg;

public class LogoutRequestHandler(IUserSessionRepository sessionRepo, ILogger<LogoutRequestHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.LogoutRequest;
    public PacketType ResponseType => PacketType.LogoutResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        if (connection.User != null)
        {
            logger.LogInformation($"[LOGOUT] User {connection.User.Username} is leaving.");
            await sessionRepo.DeleteAllForUserAsync(connection.User.Id, ct);
        }

        await connection.SendAsync(ResponseType, new LogoutResponse().ToBytes(), ct);
        
        await Task.Delay(500, ct);
        connection.Stream.Close();
    }
}