using AISpace.Common.Network.Packets.Msg;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers.Msg;

public class CircleChatInHandler(ILogger<CircleChatInHandler> logger) : PacketHandlerBase<CircleChatInRequest, CircleChatInResponse>
{
    public override PacketType RequestType => PacketType.CircleChatInRequest;
    public override PacketType ResponseType => PacketType.CircleChatInResponse;
    public override MessageDomain Domain => MessageDomain.Msg;

    public override async Task<CircleChatInResponse?> HandleAsync(CircleChatInRequest request, ClientConnection connection, CancellationToken ct = default)
    {
        // Логика:
        // 1. Проверить, состоит ли игрок в указанной гильдии (request.CircleId).
        // 2. Если да, пометить соединение как "в чате гильдии" (например, connection.ActiveCircleId = request.CircleId).
        // 3. Вернуть успешный ответ.

        // В вашем случае, если пока нет полной поддержки гильдий в памяти, просто возвращаем успех.
        logger.LogInformation($"Player {connection.CharacterId} entering circle chat {request.CircleId}");
        
        return new CircleChatInResponse(0); // 0 = успех
    }
}