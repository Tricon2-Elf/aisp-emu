using AISpace.Common.Game;
using AISpace.Network;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaEventFadeOutHandler(ILogger<AreaEventFadeOutHandler> logger)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.EventFadeOutRequest;
    public PacketType ResponseType => (PacketType)0;
    public ServerType ServerType => ServerType.Area;

    public Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        logger.LogDebug("EventFadeOut from character {CharacterId}", session.CharacterId);
        return Task.CompletedTask;
    }
}
