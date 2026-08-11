using AISpace.Common;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Common;
using Microsoft.Extensions.Logging;

namespace AISpace.Server.Services;

/// <summary>Disconnects sessions owned by one game-server type. Used only by protected portal APIs.</summary>
public sealed class ServerTypeSessionService(SharedState state, ILogger<ServerTypeSessionService> logger)
{
    public async Task<int> DisconnectUserAsync(int userId, ServerType serverType, CancellationToken ct)
    {
        var sessions = state.GetServerClients(serverType).Where(session => session.UserId == userId).ToArray();
        var logoutData = new LogoutResponse().ToBytes();

        foreach (var session in sessions)
        {
            try
            {
                await session.SendAsync(PacketType.LogoutResponse, logoutData, ct);
                if (session is PlayerSession playerSession)
                    playerSession.ClientConnection.Stream.Close();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to disconnect user {UserId} from {ServerType}", userId, serverType);
            }
        }

        return sessions.Length;
    }
}
