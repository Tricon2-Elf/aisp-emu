using aisp.Common.DAL.Entities;
using aisp.Common.Services.Toxicity;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.DAL.Repositories;

public interface IChatLogRepository
{
    Task AddAsync(ChatMessage message, CancellationToken ct = default);

    Task SetToxicityAsync(long id, bool toxicity, string reason, CancellationToken ct = default);

    Task<IReadOnlyList<ChatMessage>> ListUnclassifiedAsync(
        int take,
        CancellationToken ct = default
    );

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

    Task<IReadOnlyList<ChatMessage>> ListRecentOnMapAsync(
        uint mapId,
        int channelId,
        DateTime sinceUtc,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<ChatMessage>> ListRecentCircleAsync(
        int circleId,
        DateTime sinceUtc,
        int take,
        CancellationToken ct = default
    );
}

public sealed class ChatLogRepository(MainContext db, IChatToxicityClassifier? toxicity = null)
    : IChatLogRepository
{
    public const int MaxPageSize = 500;

    public async Task AddAsync(ChatMessage message, CancellationToken ct = default)
    {
        if (message.CreatedAt == default)
            message.CreatedAt = DateTime.UtcNow;
        db.ChatMessages.Add(message);
        await db.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(message.Message))
            toxicity?.TryEnqueue(message.Id, message.Message);
    }

    public Task SetToxicityAsync(
        long id,
        bool toxicity,
        string reason,
        CancellationToken ct = default
    )
    {
        var truncated =
            reason.Length <= ChatToxicityReason.MaxLength
                ? reason
                : reason[..ChatToxicityReason.MaxLength];
        return db
            .ChatMessages.Where(x => x.Id == id)
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(x => x.Toxicity, toxicity)
                        .SetProperty(x => x.ToxicityReason, truncated),
                ct
            );
    }

    public async Task<IReadOnlyList<ChatMessage>> ListUnclassifiedAsync(
        int take,
        CancellationToken ct = default
    )
    {
        var pageSize = Math.Clamp(take, 1, MaxPageSize);
        return await db
            .ChatMessages.AsNoTracking()
            .Where(x => x.ToxicityReason == "" && x.Message.Trim() != "")
            .OrderByDescending(x => x.Id)
            .Take(pageSize)
            .ToListAsync(ct);
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

    public async Task<IReadOnlyList<ChatMessage>> ListRecentOnMapAsync(
        uint mapId,
        int channelId,
        DateTime sinceUtc,
        CancellationToken ct = default
    ) =>
        await db
            .ChatMessages.AsNoTracking()
            .Where(x =>
                (x.Kind == ChatLogKind.Public || x.Kind == ChatLogKind.Placard)
                && x.MapId == mapId
                && x.ChannelId == channelId
                && x.CreatedAt >= sinceUtc
            )
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ChatMessage>> ListRecentCircleAsync(
        int circleId,
        DateTime sinceUtc,
        int take,
        CancellationToken ct = default
    )
    {
        var pageSize = Math.Clamp(take, 1, MaxPageSize);
        var newest = await db
            .ChatMessages.AsNoTracking()
            .Where(x =>
                x.Kind == ChatLogKind.Circle
                && x.CircleId == circleId
                && !x.Rejected
                && x.CreatedAt >= sinceUtc
            )
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(pageSize)
            .ToListAsync(ct);

        newest.Reverse();
        return newest;
    }
}
