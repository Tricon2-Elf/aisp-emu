using AISpace.Network.Packets.Common;
using AISpace.Network;
using AISpace.Common.Game;
using AISpace.Common.DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Msg;

public class LogoutRequestHandler(IUserSessionRepository sessionRepo, ILogger<LogoutRequestHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.LogoutRequest;
    public PacketType ResponseType => PacketType.LogoutResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        if (session.User != null)
        {
            logger.LogInformation($"[LOGOUT] User {session.User.Username} is leaving.");
            await sessionRepo.DeleteAllForUserAsync(session.User.Id, ct);
        }

        await session.SendAsync(ResponseType, new LogoutResponse().ToBytes(), ct);

        await Task.Delay(500, ct);
    }
}
