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
}
