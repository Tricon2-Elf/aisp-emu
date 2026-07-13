using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Services;

public class ScriptedEventTriggerService(ICharacterEventRepository eventRepository, ILogger<ScriptedEventTriggerService> logger)
{
    public async Task<bool> TryStartOnMovementAsync(IPlayerSession session, IReadOnlyList<MovementPositionSample> samples, CancellationToken ct = default)
    {
        if (session.ActiveEventKey != null || session.CharacterId == 0 || session.MapId == 0 || samples.Count == 0)
            return false;

        var characterId = (int)session.CharacterId;

        foreach (var trigger in ScriptedEventTriggers.OnMovement)
        {
            if (trigger.MapId != session.MapId)
                continue;

            if (await eventRepository.HasCompletedAsync(characterId, trigger.EventKey, ct))
                continue;

            if (!samples.Any(sample => ScriptedEventTriggers.IsWithinRadius(sample, trigger)))
                continue;

            logger.LogInformation("Starting scripted event {EventKey} for character {CharacterId} after entering marker on map {MapId}", trigger.EventKey, session.CharacterId, session.MapId);
            await ClientScriptLauncher.StartAsync(session, trigger.EventKey, ct: ct);
            return true;
        }

        return false;
    }
}
