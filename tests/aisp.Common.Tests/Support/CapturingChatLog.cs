using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;

namespace aisp.Common.Tests.Support;

internal sealed class CapturingChatLog : IChatLogRepository
{
    public List<ChatMessage> Entries { get; } = [];

    public Task AddAsync(ChatMessage message, CancellationToken ct = default)
    {
        if (message.CreatedAt == default)
            message.CreatedAt = DateTime.UtcNow;
        message.Id = Entries.Count + 1;
        Entries.Add(message);
        return Task.CompletedTask;
    }

    public Task<(IReadOnlyList<ChatMessage> Items, int Total)> ListAsync(
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
        IEnumerable<ChatMessage> query = Entries;
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

        var matched = query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .ToList();
        var page = matched.Skip(Math.Max(skip, 0)).Take(Math.Clamp(take, 1, 500)).ToList();
        return Task.FromResult<(IReadOnlyList<ChatMessage>, int)>((page, matched.Count));
    }

    public Task<int> PruneOlderThanAsync(DateTime cutoffUtc, CancellationToken ct = default)
    {
        var removed = Entries.RemoveAll(x => x.CreatedAt < cutoffUtc);
        return Task.FromResult(removed);
    }

    public Task<IReadOnlyList<ChatMessage>> ListRecentOnMapAsync(
        uint mapId,
        int channelId,
        DateTime sinceUtc,
        CancellationToken ct = default
    )
    {
        var items = Entries
            .Where(x =>
                (x.Kind == ChatLogKind.Public || x.Kind == ChatLogKind.Placard)
                && x.MapId == mapId
                && x.ChannelId == channelId
                && x.CreatedAt >= sinceUtc
            )
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToList();
        return Task.FromResult<IReadOnlyList<ChatMessage>>(items);
    }
}
