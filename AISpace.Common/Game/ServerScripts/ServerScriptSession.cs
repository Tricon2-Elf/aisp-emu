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

    public async Task CompleteAsync(IPlayerSession session, uint result, bool markComplete, CancellationToken ct = default)
    {
        var eventKey = session.ActiveEventKey;
        session.ActiveEventKey = null;
        session.ActiveEventKind = NpcEventKind.None;
        session.ServerScriptState = null;

        await session.SendAsync(PacketType.EventEndNotify, new EventEndNotify(result).ToBytes(), ct);

        if (markComplete && eventKey is not null && session.CharacterId != 0)
        {
            await eventRepository.MarkCompletedAsync((int)session.CharacterId, eventKey, ct);
            logger.LogInformation("Marked server script {EventKey} complete for character {CharacterId}", eventKey, session.CharacterId);
        }
    }

    public Task AbortAsync(IPlayerSession session, uint result, CancellationToken ct = default) => CompleteAsync(session, result, markComplete: false, ct);
}
