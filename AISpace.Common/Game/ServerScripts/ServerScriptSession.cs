using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Game.ServerScripts;

public sealed class ServerScriptSession(ICharacterEventRepository eventRepository, ILogger<ServerScriptSession> logger)
{
    public void Begin(IPlayerSession session, string eventKey)
    {
        session.ActiveEventKey = eventKey;
        session.ActiveEventKind = NpcEventKind.ServerScript;
        session.ServerScriptState = new ServerScriptState { EventKey = eventKey, Step = string.Empty };
    }

    public Task CompleteAsync(IPlayerSession session, uint result, bool markComplete, CancellationToken ct = default) =>
        CompleteAsync(session, result, markComplete, completionEventKey: null, ct);

    public async Task CompleteAsync(IPlayerSession session, uint result, bool markComplete, string? completionEventKey, CancellationToken ct = default)
    {
        var eventKey = session.ActiveEventKey;
        session.ActiveEventKey = null;
        session.ActiveEventKind = NpcEventKind.None;
        session.ServerScriptState = null;

        await session.SendAsync(PacketType.EventEndNotify, new EventEndNotify(result).ToBytes(), ct);

        if (markComplete && session.CharacterId != 0)
        {
            var keyToMark = completionEventKey ?? eventKey;
            if (keyToMark is not null)
            {
                await eventRepository.MarkCompletedAsync((int)session.CharacterId, keyToMark, ct);
                logger.LogInformation("Marked server script {EventKey} complete for character {CharacterId}", keyToMark, session.CharacterId);
            }
        }
    }

    public Task AbortAsync(IPlayerSession session, uint result, CancellationToken ct = default) => CompleteAsync(session, result, markComplete: false, ct);
}
