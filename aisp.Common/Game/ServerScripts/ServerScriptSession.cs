using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Game.ServerScripts;

public sealed class ServerScriptSession(
    ICharacterEventRepository eventRepository,
    ILogger<ServerScriptSession> logger
)
{
    public void Begin(
        IPlayerSession session,
        string eventKey,
        EventCompletionPolicy completionPolicy
    )
    {
        session.ActiveEventKey = eventKey;
        session.ActiveEventKind = NpcEventKind.ServerScript;
        session.ActiveEventCompletionPolicy = completionPolicy;
        session.ServerScriptState = new ServerScriptState
        {
            EventKey = eventKey,
            Step = string.Empty,
        };
    }

    public async Task CompleteAsync(
        IPlayerSession session,
        uint result,
        bool markComplete,
        CancellationToken ct = default
    )
    {
        var eventKey = session.ActiveEventKey;
        var completionPolicy = session.ActiveEventCompletionPolicy;
        session.ActiveEventKey = null;
        session.ActiveEventKind = NpcEventKind.None;
        session.ActiveEventCompletionPolicy = EventCompletionPolicy.Once;
        session.ServerScriptState = null;

        await session.SendAsync(
            PacketType.EventEndNotify,
            new EventEndNotify(result).ToBytes(),
            ct
        );

        if (
            markComplete
            && completionPolicy == EventCompletionPolicy.Once
            && session.CharacterId != 0
            && eventKey is not null
        )
        {
            await eventRepository.MarkCompletedAsync((int)session.CharacterId, eventKey, ct);
            logger.LogInformation(
                "Marked server script {EventKey} complete for character {CharacterId}",
                eventKey,
                session.CharacterId
            );
        }
    }

    public Task AbortAsync(IPlayerSession session, uint result, CancellationToken ct = default) =>
        CompleteAsync(session, result, markComplete: false, ct);
}
