using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Game.ServerScripts;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaEventFadeInHandler(
    ICharacterEventRepository eventRepository,
    ILogger<AreaEventFadeInHandler> logger,
    ServerScriptDispatcher? serverScriptDispatcher = null
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.EventFadeInRequest;
    public PacketType ResponseType => PacketType.EventEndNotify;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (
            serverScriptDispatcher is not null
            && await serverScriptDispatcher.TryHandlePacketAsync(RequestType, payload, session, ct)
        )
            return;

        if (!session.PendingEventEndAfterFade)
        {
            logger.LogDebug("EventFadeIn from character {CharacterId}", session.CharacterId);
            return;
        }

        session.PendingEventEndAfterFade = false;
        logger.LogInformation(
            "EventFadeIn from character {CharacterId}: ending pending event",
            session.CharacterId
        );
        await session.SendAsync(PacketType.EventEndNotify, new EventEndNotify(0).ToBytes(), ct);

        if (
            session.ActiveEventKind != NpcEventKind.ClientScript
            || session.ActiveEventKey is not { } eventKey
        )
            return;

        var shouldMarkComplete = session.ActiveEventCompletionPolicy == EventCompletionPolicy.Once;
        session.ActiveEventKey = null;
        session.ActiveEventKind = NpcEventKind.None;
        session.ActiveEventCompletionPolicy = EventCompletionPolicy.Once;

        if (!shouldMarkComplete)
            return;

        await eventRepository.MarkCompletedAsync((int)session.CharacterId, eventKey, ct);
        logger.LogInformation(
            "Marked client script {EventKey} complete for character {CharacterId}",
            eventKey,
            session.CharacterId
        );
    }
}
