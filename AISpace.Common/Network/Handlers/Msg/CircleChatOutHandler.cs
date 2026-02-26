using AISpace.Common.Network.Packets.Msg;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers.Msg;

public class CircleChatOutHandler(ILogger<CircleChatOutHandler> logger) : PacketHandlerBase<CircleChatOutRequest, CircleChatInResponse> // Use the same response type if the format matches
{
    public override PacketType RequestType => PacketType.CircleChatOutRequest;
    public override PacketType ResponseType => PacketType.CircleChatInResponse; // Often the same response (just Result)
    public override MessageDomain Domain => MessageDomain.Msg;

    public override async Task<CircleChatInResponse?> HandleAsync(CircleChatOutRequest request, ClientConnection connection, CancellationToken ct = default)
    {
        logger.LogInformation($"Player {connection.CharacterId} leaving circle chat");
        // Exit logic
        return new CircleChatInResponse(0);
    }
}
