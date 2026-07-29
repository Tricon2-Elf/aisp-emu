using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Msg;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Msg;

public class CircleChatOutHandler(ILogger<CircleChatOutHandler> logger)
    : PacketHandlerBase<CircleChatOutRequest, CircleChatInResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.CircleChatOutRequest;
    public override PacketType ResponseType => PacketType.CircleChatInResponse; // Often the same response (just Result)
    public override ServerType ServerType => ServerType.Msg;

    public override async Task<CircleChatInResponse?> HandleAsync(
        CircleChatOutRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        logger.LogInformation($"Player {session.CharacterId} leaving circle chat");
        // Exit logic
        return new CircleChatInResponse(0);
    }
}
