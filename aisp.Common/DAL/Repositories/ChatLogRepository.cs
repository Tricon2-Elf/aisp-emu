using aisp.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.DAL.Repositories;

public interface IChatLogRepository
{
    Task AddAsync(ChatMessage message, CancellationToken ct = default);

    Task<(IReadOnlyList<ChatMessage> Items, int Total)> ListAsync(
        ChatLogKind? kind = null,
        int? userId = null,
        int? characterId = null,
        int? circleId = null,
        bool? rejected = null,
        int skip = 0,
        int take = 100,
        CancellationToken ct = default
    );

    Task<int> PruneOlderThanAsync(DateTime cutoffUtc, CancellationToken ct = default);
}

public sealed class ChatLogRepository(MainContext db) : IChatLogRepository
{
    public const int MaxPageSize = 500;

    public async Task AddAsync(ChatMessage message, CancellationToken ct = default)
    {
        if (message.CreatedAt == default)
            message.CreatedAt = DateTime.UtcNow;
        db.ChatMessages.Add(message);
        await db.SaveChangesAsync(ct);
    }

    public async Task<(IReadOnlyList<ChatMessage> Items, int Total)> ListAsync(
        ChatLogKind? kind = null,
        int? userId = null,
        int? characterId = null,
        int? circleId = null,
        bool? rejected = null,
        int skip = 0,
        int take = 100,
        CancellationToken ct = default
    )
    {
        var query = db.ChatMessages.AsNoTracking();
        if (kind.HasValue)
            query = query.Where(x => x.Kind == kind.Value);
        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);
        if (characterId.HasValue)
            query = query.Where(x => x.CharacterId == characterId.Value);
        if (circleId.HasValue)
            query = query.Where(x => x.CircleId == circleId.Value);
        if (rejected.HasValue)
            query = query.Where(x => x.Rejected == rejected.Value);

        var total = await query.CountAsync(ct);
        var pageSize = Math.Clamp(take, 1, MaxPageSize);
        var offset = Math.Max(skip, 0);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip(offset)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public Task<int> PruneOlderThanAsync(DateTime cutoffUtc, CancellationToken ct = default) =>
        db.ChatMessages.Where(x => x.CreatedAt < cutoffUtc).ExecuteDeleteAsync(ct);
}
