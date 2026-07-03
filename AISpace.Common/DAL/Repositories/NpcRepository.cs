using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.DAL.Repositories;

public interface INpcRepository
{
    Task<IReadOnlyList<Npc>> GetActiveByMapAsync(uint mapId, CancellationToken ct = default);
    Task<Npc?> GetActiveByMapAndObjectIdAsync(uint mapId, uint npcObjectId, CancellationToken ct = default);
    Task<Shop?> GetSingleActiveShopForMapAsync(uint mapId, CancellationToken ct = default);
}

public sealed class NpcRepository(MainContext db) : INpcRepository
{
    public async Task<IReadOnlyList<Npc>> GetActiveByMapAsync(uint mapId, CancellationToken ct = default)
    {
        var activePhase = (int)TimeZoneService.GetServerTime().Phase;
        var nowUtc = DateTime.UtcNow;
        long mapIdLong = mapId;
        return await db
            .Npcs.AsNoTracking()
            .Include(x => x.Equipment)
            .Where(x =>
                x.IsEnabled
                && x.MapId == mapIdLong
                && (x.DayPhase == -1 || x.DayPhase == activePhase)
                && x.DateStartUtc <= nowUtc
                && x.DateEndUtc >= nowUtc
            )
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }

    public async Task<Npc?> GetActiveByMapAndObjectIdAsync(uint mapId, uint npcObjectId, CancellationToken ct = default)
    {
        var activePhase = (int)TimeZoneService.GetServerTime().Phase;
        var nowUtc = DateTime.UtcNow;
        long mapIdLong = mapId;
        long npcObjectIdLong = npcObjectId;
        return await db
            .Npcs.AsNoTracking()
            .Include(x => x.Shop)
            .FirstOrDefaultAsync(
                x =>
                    x.IsEnabled
                    && x.MapId == mapIdLong
                    && x.NpcObjectId == npcObjectIdLong
                    && (x.DayPhase == -1 || x.DayPhase == activePhase)
                    && x.DateStartUtc <= nowUtc
                    && x.DateEndUtc >= nowUtc,
                ct
            );
    }

    public async Task<Shop?> GetSingleActiveShopForMapAsync(uint mapId, CancellationToken ct = default)
    {
        var activePhase = (int)TimeZoneService.GetServerTime().Phase;
        var nowUtc = DateTime.UtcNow;
        long mapIdLong = mapId;
        var shopIds = await db
            .Npcs.AsNoTracking()
            .Where(x =>
                x.IsEnabled
                && x.MapId == mapIdLong
                && x.ShopId != null
                && (x.DayPhase == -1 || x.DayPhase == activePhase)
                && x.DateStartUtc <= nowUtc
                && x.DateEndUtc >= nowUtc
            )
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .Select(x => x.ShopId!.Value)
            .Distinct()
            .Take(2)
            .ToListAsync(ct);

        if (shopIds.Count != 1)
            return null;

        var shopId = shopIds[0];
        return await db.Shops.AsNoTracking().FirstOrDefaultAsync(x => x.Id == shopId && x.IsEnabled, ct);
    }
}
