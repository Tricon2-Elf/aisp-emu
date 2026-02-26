using AISpace.Common.Network.Packets.Msg;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers.Msg;

public class CircleChatOutHandler(ILogger<CircleChatOutHandler> logger) : PacketHandlerBase<CircleChatOutRequest, CircleChatInResponse> // Используем тот же ответ, если формат совпадает
{
    public override PacketType RequestType => PacketType.CircleChatOutRequest;
    public override PacketType ResponseType => PacketType.CircleChatInResponse; // Часто ответ тот же (просто Result)
    public override MessageDomain Domain => MessageDomain.Msg;

    public override async Task<CircleChatInResponse?> HandleAsync(CircleChatOutRequest request, ClientConnection connection, CancellationToken ct = default)
    {
        logger.LogInformation($"Player {connection.CharacterId} leaving circle chat");
        // Логика выхода
        return new CircleChatInResponse(0);
    }
}