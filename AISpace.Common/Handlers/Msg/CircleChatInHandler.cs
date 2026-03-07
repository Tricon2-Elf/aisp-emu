using AISpace.Network;
using AISpace.Network.Packets.Msg;

namespace AISpace.Common.Handlers.Msg;

public class CircleChatInHandler(ILogger<CircleChatInHandler> logger) : PacketHandlerBase<CircleChatInRequest, CircleChatInResponse>
{
    public override PacketType RequestType => PacketType.CircleChatInRequest;
    public override PacketType ResponseType => PacketType.CircleChatInResponse;
    public override MessageDomain Domain => MessageDomain.Msg;

    public override async Task<CircleChatInResponse?> HandleAsync(CircleChatInRequest request, ClientConnection connection, CancellationToken ct = default)
    {
        // Логика:
        // 1. Check if the player is in the specified circle (request.CircleId).
        // 2. If yes, mark the connection as "in circle chat" (e.g. connection.ActiveCircleId = request.CircleId).
        // 3. Return success.

        // If there is no full support for circles in memory, simply return success.
        logger.LogInformation($"Player {connection.CharacterId} entering circle chat {request.CircleId}");

        return new CircleChatInResponse(0); // 0 = success
    }
}
