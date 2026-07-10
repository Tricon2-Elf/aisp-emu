using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaEventFadeInHandler(ILogger<AreaEventFadeInHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.EventFadeInRequest;
    public PacketType ResponseType => PacketType.EventEndNotify;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        if (!session.PendingEventEndAfterFade)
        {
            logger.LogDebug("EventFadeIn from character {CharacterId}", session.CharacterId);
            return;
        }

        session.PendingEventEndAfterFade = false;
        logger.LogInformation("EventFadeIn from character {CharacterId}: ending pending event", session.CharacterId);
        await session.SendAsync(PacketType.EventEndNotify, new EventEndNotify(0).ToBytes(), ct);
    }
}
