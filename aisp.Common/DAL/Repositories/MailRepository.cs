using aisp.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.DAL.Repositories;

public sealed record MailboxEntry(MailMessage Message, uint Type, bool IsRead);

public interface IMailRepository
{
    Task<MailMessage> AddAsync(MailMessage message, CancellationToken ct = default);
    Task<IReadOnlyList<MailboxEntry>> ListRecentAsync(
        int characterId,
        int take,
        CancellationToken ct = default
    );
    Task<bool> DeleteAsync(int characterId, long mailId, uint type, CancellationToken ct = default);
    Task<bool> SetProtectedAsync(
        int characterId,
        long mailId,
        bool isProtected,
        CancellationToken ct = default
    );
    Task<bool> MarkReadAsync(
        int characterId,
        long mailId,
        uint type,
        CancellationToken ct = default
    );
}

public sealed class MailRepository(MainContext db) : IMailRepository
{
    public const int MaxPageSize = 350;

    public async Task<MailMessage> AddAsync(MailMessage message, CancellationToken ct = default)
    {
        if (message.CreatedAtUtc == default)
            message.CreatedAtUtc = DateTime.UtcNow;

        db.MailMessages.Add(message);
        await db.SaveChangesAsync(ct);
        return message;
    }

    public async Task<IReadOnlyList<MailboxEntry>> ListRecentAsync(
        int characterId,
        int take,
        CancellationToken ct = default
    )
    {
        var pageSize = Math.Clamp(take, 1, MaxPageSize);
        var protectedMail = await db
            .MailRecipients.AsNoTracking()
            .Include(x => x.MailMessage)
            .Where(x => x.CharacterId == characterId && !x.IsDeleted && x.IsProtected)
            .OrderByDescending(x => x.MailMessage.CreatedAtUtc)
            .ThenByDescending(x => x.MailMessageId)
            .Take(pageSize)
            .ToListAsync(ct);
        var protectedEntries = protectedMail
            .Select(x => new MailboxEntry(x.MailMessage, 3, x.IsRead))
            .ToList();
        var remaining = pageSize - protectedEntries.Count;
        if (remaining == 0)
            return protectedEntries;

        var sent = await db
            .MailMessages.AsNoTracking()
            .Where(x => x.SenderCharacterId == characterId && !x.SenderDeleted)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Take(remaining)
            .ToListAsync(ct);
        var received = await db
            .MailRecipients.AsNoTracking()
            .Include(x => x.MailMessage)
            .Where(x => x.CharacterId == characterId && !x.IsDeleted && !x.IsProtected)
            .OrderByDescending(x => x.MailMessage.CreatedAtUtc)
            .ThenByDescending(x => x.MailMessageId)
            .Take(remaining)
            .ToListAsync(ct);

        var newestUnprotected = sent.Select(x => new MailboxEntry(x, 1, false))
            .Concat(received.Select(x => new MailboxEntry(x.MailMessage, x.InboxType, x.IsRead)))
            .OrderByDescending(x => x.Message.CreatedAtUtc)
            .ThenByDescending(x => x.Message.Id)
            .Take(remaining);

        return protectedEntries.Concat(newestUnprotected).ToList();
    }

    public async Task<bool> DeleteAsync(
        int characterId,
        long mailId,
        uint type,
        CancellationToken ct = default
    )
    {
        if (type == 1)
        {
            return await db
                    .MailMessages.Where(x =>
                        x.Id == mailId && x.SenderCharacterId == characterId && !x.SenderDeleted
                    )
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.SenderDeleted, true), ct) == 1;
        }

        if (type > 3)
            return false;

        return await db
                .MailRecipients.Where(x =>
                    x.MailMessageId == mailId && x.CharacterId == characterId && !x.IsDeleted
                )
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, true), ct) == 1;
    }

    public async Task<bool> SetProtectedAsync(
        int characterId,
        long mailId,
        bool isProtected,
        CancellationToken ct = default
    )
    {
        var recipients = db.MailRecipients.Where(x =>
            x.MailMessageId == mailId && x.CharacterId == characterId && !x.IsDeleted
        );

        return isProtected
            ? await recipients.ExecuteUpdateAsync(s => s.SetProperty(x => x.IsProtected, true), ct)
                == 1
            : await recipients.ExecuteUpdateAsync(
                s =>
                    s.SetProperty(x => x.IsProtected, false).SetProperty(x => x.InboxType, (byte)0),
                ct
            ) == 1;
    }

    public async Task<bool> MarkReadAsync(
        int characterId,
        long mailId,
        uint type,
        CancellationToken ct = default
    )
    {
        if (type == 1)
        {
            return await db.MailMessages.AnyAsync(
                x => x.Id == mailId && x.SenderCharacterId == characterId && !x.SenderDeleted,
                ct
            );
        }

        if (type > 3)
            return false;

        return await db
                .MailRecipients.Where(x =>
                    x.MailMessageId == mailId && x.CharacterId == characterId && !x.IsDeleted
                )
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true), ct) == 1;
    }
}
