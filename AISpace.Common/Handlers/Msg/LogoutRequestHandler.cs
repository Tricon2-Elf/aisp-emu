using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Common;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Msg;

public class LogoutRequestHandler(IUserSessionRepository sessionRepo, ILogger<LogoutRequestHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.LogoutRequest;
    public PacketType ResponseType => PacketType.LogoutResponse;
    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        logger.LogInformation("[LOGOUT] User {username} is leaving.", session.User!.Username);
        await sessionRepo.DeleteAllForUserAsync(session.User.Id, ct);
        await session.SendAsync(ResponseType, new LogoutResponse().ToBytes(), ct);

        await Task.Delay(500, ct);
    }
}
