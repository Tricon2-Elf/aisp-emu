using AISpace.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.DAL.Repositories;

public interface ICharacterEventRepository
{
    Task<bool> HasCompletedAsync(int characterId, string eventKey, CancellationToken ct = default);
    Task<IReadOnlySet<string>> GetCompletedEventKeysAsync(int characterId, CancellationToken ct = default);
    Task MarkCompletedAsync(int characterId, string eventKey, CancellationToken ct = default);
}

public sealed class CharacterEventRepository(MainContext db) : ICharacterEventRepository
{
    public async Task<bool> HasCompletedAsync(int characterId, string eventKey, CancellationToken ct = default) => await db.CharacterEventStatuses.AnyAsync(x => x.CharacterId == characterId && x.EventKey == eventKey, ct);

    public async Task<IReadOnlySet<string>> GetCompletedEventKeysAsync(int characterId, CancellationToken ct = default)
    {
        var keys = await db.CharacterEventStatuses.Where(x => x.CharacterId == characterId).Select(x => x.EventKey).ToListAsync(ct);
        return keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task MarkCompletedAsync(int characterId, string eventKey, CancellationToken ct = default)
    {
        if (await HasCompletedAsync(characterId, eventKey, ct))
            return;

        db.CharacterEventStatuses.Add(
            new CharacterEventStatus
            {
                CharacterId = characterId,
                EventKey = eventKey,
                CompletedAtUtc = DateTime.UtcNow,
            }
        );
        await db.SaveChangesAsync(ct);
    }
}
