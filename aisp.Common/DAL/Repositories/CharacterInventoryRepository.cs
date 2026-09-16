using System.Collections.Concurrent;
using aisp.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.DAL.Repositories;

/// <summary>
/// Repository boundary for saving operations that can add character inventory stacks.
/// </summary>
internal static class CharacterInventoryRepository
{
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> CapacityLocks = new();

    public static async Task<bool> TrySaveChangesAsync(
        MainContext db,
        CancellationToken ct = default
    )
    {
        var increases = GetStackIncreases(db);
        var heldLocks = new List<SemaphoreSlim>();

        try
        {
            foreach (var characterId in increases.Keys.Order())
            {
                var capacityLock = CapacityLocks.GetOrAdd(
                    characterId,
                    static _ => new SemaphoreSlim(1, 1)
                );
                await capacityLock.WaitAsync(ct);
                heldLocks.Add(capacityLock);
            }

            foreach (var (characterId, increase) in increases)
            {
                var persistedStacks = await db
                    .CharacterInventories.AsNoTracking()
                    .CountAsync(x => x.CharacterId == characterId && x.Quantity > 0, ct);
                if (persistedStacks + increase > CharacterRepository.MaximumInventoryStacks)
                {
                    db.ChangeTracker.Clear();
                    return false;
                }
            }

            await db.SaveChangesAsync(ct);
            return true;
        }
        finally
        {
            foreach (var capacityLock in heldLocks.AsEnumerable().Reverse())
                capacityLock.Release();
        }
    }

    private static Dictionary<int, int> GetStackIncreases(MainContext db)
    {
        db.ChangeTracker.DetectChanges();
        return db
            .ChangeTracker.Entries<CharacterInventory>()
            .Where(entry =>
                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
            )
            .GroupBy(entry => entry.Entity.CharacterId)
            .Select(group => new
            {
                CharacterId = group.Key,
                Delta = group.Sum(entry =>
                    entry.State switch
                    {
                        EntityState.Added => entry.CurrentValues.GetValue<int>(
                            nameof(CharacterInventory.Quantity)
                        ) > 0
                            ? 1
                            : 0,
                        EntityState.Deleted => entry.OriginalValues.GetValue<int>(
                            nameof(CharacterInventory.Quantity)
                        ) > 0
                            ? -1
                            : 0,
                        EntityState.Modified => (
                            entry.CurrentValues.GetValue<int>(nameof(CharacterInventory.Quantity))
                            > 0
                                ? 1
                                : 0
                        )
                            - (
                                entry.OriginalValues.GetValue<int>(
                                    nameof(CharacterInventory.Quantity)
                                ) > 0
                                    ? 1
                                    : 0
                            ),
                        _ => 0,
                    }
                ),
            })
            .Where(change => change.Delta > 0)
            .ToDictionary(change => change.CharacterId, change => change.Delta);
    }
}
