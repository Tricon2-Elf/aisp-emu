using System.Data;
using aisp.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.DAL.Repositories;

public interface IAdventureWorkRepository
{
    Task<(int SheetStock, IReadOnlyList<AdventureWork> Works)> GetWorksAsync(
        int userId,
        CancellationToken ct = default
    );

    /// <summary>Allocates the next work id and takes <paramref name="sheets"/> from the account's stock. Null when the stock is short or the client-side limit of 100 works is reached.</summary>
    Task<(AdventureWork? Work, int SheetStock)> CreateAsync(
        int userId,
        int characterId,
        int sheets,
        CancellationToken ct = default
    );

    /// <summary>Removes the work and returns its sheets to the stock.</summary>
    Task<(bool Removed, int SheetStock)> DeleteAsync(
        int userId,
        int workId,
        CancellationToken ct = default
    );

    /// <summary>Moves <paramref name="delta"/> sheets between the stock and the work (positive adds to the work). Null when the work is missing or the stock or the work would go below zero.</summary>
    Task<(AdventureWork? Work, int SheetStock)> AdjustSheetsAsync(
        int userId,
        int workId,
        int delta,
        CancellationToken ct = default
    );

    /// <summary>
    /// Registers (or updates) a work id the client already holds locally, e.g. a manuscript restored from a backup,
    /// and moves the id counter past it so recv_adventure_work_create_r can never hand that id out again. Does not
    /// touch the sheet stock.
    /// </summary>
    Task<AdventureWork?> RegisterAsync(
        int userId,
        int characterId,
        int workId,
        int sheets,
        CancellationToken ct = default
    );

    /// <summary>Adds sheets to the account's stock (the shop-bought 原稿用紙). Returns the new stock, or null when the user is unknown.</summary>
    Task<int?> AddSheetsAsync(int userId, int count, CancellationToken ct = default);

    /// <summary>Buys sheets for デレ at the sheet shop. Null when the user is unknown, the count is not positive, the stock would pass <see cref="AdventureWorkRepository.MaxSheetStock"/>, or the purse is short.</summary>
    Task<(int SheetStock, long AiPoints)?> BuySheetsAsync(
        int userId,
        int count,
        long unitPrice,
        CancellationToken ct = default
    );
}

public sealed class AdventureWorkRepository(MainContext db) : IAdventureWorkRepository
{
    /// <summary>recv_get_adventure_work_list_r carries at most 100 records.</summary>
    public const int MaxWorksPerUser = 100;

    public async Task<(int SheetStock, IReadOnlyList<AdventureWork> Works)> GetWorksAsync(
        int userId,
        CancellationToken ct = default
    )
    {
        var stock =
            await db
                .Users.Where(u => u.Id == userId)
                .Select(u => (int?)u.AdventureSheetStock)
                .SingleOrDefaultAsync(ct) ?? 0;
        var works = await db
            .AdventureWorks.Where(w => w.UserId == userId)
            .OrderBy(w => w.WorkId)
            .AsNoTracking()
            .ToListAsync(ct);
        return (stock, works);
    }

    public async Task<(AdventureWork? Work, int SheetStock)> CreateAsync(
        int userId,
        int characterId,
        int sheets,
        CancellationToken ct = default
    )
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return (null, 0);
        var count = await db.AdventureWorks.CountAsync(w => w.UserId == userId, ct);
        if (
            sheets <= 0
            || sheets > user.AdventureSheetStock
            || count >= MaxWorksPerUser
            || user.NextAdventureWorkId >= ushort.MaxValue
        )
            return (null, user.AdventureSheetStock);
        var work = new AdventureWork
        {
            UserId = userId,
            CharacterId = characterId,
            WorkId = user.NextAdventureWorkId,
            Sheets = sheets,
            CreatedAt = DateTime.UtcNow,
        };
        user.NextAdventureWorkId++;
        user.AdventureSheetStock -= sheets;
        db.AdventureWorks.Add(work);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return (work, user.AdventureSheetStock);
    }

    public async Task<(bool Removed, int SheetStock)> DeleteAsync(
        int userId,
        int workId,
        CancellationToken ct = default
    )
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return (false, 0);
        var work = await db.AdventureWorks.SingleOrDefaultAsync(
            w => w.UserId == userId && w.WorkId == workId,
            ct
        );
        if (work is null)
            return (false, user.AdventureSheetStock);
        user.AdventureSheetStock += work.Sheets;
        db.AdventureWorks.Remove(work);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return (true, user.AdventureSheetStock);
    }

    /// <summary>The sheet shop window clamps a purchase so the stock stays below 10000.</summary>
    public const int MaxSheetStock = 9999;

    public async Task<(int SheetStock, long AiPoints)?> BuySheetsAsync(
        int userId,
        int count,
        long unitPrice,
        CancellationToken ct = default
    )
    {
        if (count <= 0 || unitPrice < 0)
            return null;
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return null;
        var total = checked(unitPrice * count);
        if (user.AdventureSheetStock + count > MaxSheetStock || user.AiPoints < total)
            return null;
        user.AdventureSheetStock += count;
        user.AiPoints -= total;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return (user.AdventureSheetStock, user.AiPoints);
    }

    public async Task<int?> AddSheetsAsync(int userId, int count, CancellationToken ct = default)
    {
        if (count <= 0)
            return null;
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return null;
        user.AdventureSheetStock = checked(user.AdventureSheetStock + count);
        await db.SaveChangesAsync(ct);
        return user.AdventureSheetStock;
    }

    public async Task<AdventureWork?> RegisterAsync(
        int userId,
        int characterId,
        int workId,
        int sheets,
        CancellationToken ct = default
    )
    {
        if (workId <= 0 || workId >= ushort.MaxValue || sheets < 0)
            return null;
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return null;
        var work = await db.AdventureWorks.SingleOrDefaultAsync(
            w => w.UserId == userId && w.WorkId == workId,
            ct
        );
        if (work is null)
        {
            if (await db.AdventureWorks.CountAsync(w => w.UserId == userId, ct) >= MaxWorksPerUser)
                return null;
            work = new AdventureWork
            {
                UserId = userId,
                CharacterId = characterId,
                WorkId = workId,
                CreatedAt = DateTime.UtcNow,
            };
            db.AdventureWorks.Add(work);
        }
        work.Sheets = sheets;
        if (user.NextAdventureWorkId <= workId)
            user.NextAdventureWorkId = workId + 1;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return work;
    }

    public async Task<(AdventureWork? Work, int SheetStock)> AdjustSheetsAsync(
        int userId,
        int workId,
        int delta,
        CancellationToken ct = default
    )
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return (null, 0);
        var work = await db.AdventureWorks.SingleOrDefaultAsync(
            w => w.UserId == userId && w.WorkId == workId,
            ct
        );
        if (work is null || user.AdventureSheetStock - delta < 0 || work.Sheets + delta < 0)
            return (null, user.AdventureSheetStock);
        work.Sheets += delta;
        user.AdventureSheetStock -= delta;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return (work, user.AdventureSheetStock);
    }
}
