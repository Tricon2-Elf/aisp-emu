using AISpace.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.DAL.Repositories;

public interface IUserSessionRepository
{
    Task<UserSession> CreateAsync(int userId, string otp, TimeSpan duration, CancellationToken ct = default);
    Task<UserSession?> GetValidSessionAsync(string otp, CancellationToken ct = default);
    Task InvalidateExpiredAsync(CancellationToken ct = default);
    Task DeleteAllForUserAsync(int userId, CancellationToken ct = default);
}

public class UserSessionRepository(MainContext db, IDbContextFactory<MainContext> factory, ILogger<UserSessionRepository> logger) : IUserSessionRepository
{
    public async Task<UserSession> CreateAsync(int userId, string otp, TimeSpan duration, CancellationToken ct = default)
    {
        await using var ctx = await factory.CreateDbContextAsync(ct);
        var session = new UserSession
        {
            UserId = userId,
            OTP = otp,
            ExpiresAt = DateTime.UtcNow.Add(duration),
        };

        ctx.UserSessions.Add(session);
        await ctx.SaveChangesAsync(ct);
        return session;
    }

    public async Task<UserSession?> GetValidSessionAsync(string otp, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await db.UserSessions.Include(s => s.User).ThenInclude(u => u.Characters).ThenInclude(c => c.Equipment).Where(s => s.OTP == otp && s.ExpiresAt > now).SingleOrDefaultAsync(ct);
    }

    public async Task InvalidateExpiredAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var expired = await db.UserSessions.Where(s => s.ExpiresAt <= now).ToListAsync(ct);

        if (expired.Count == 0)
            return;

        db.UserSessions.RemoveRange(expired);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAllForUserAsync(int userId, CancellationToken ct = default)
    {
        logger.LogInformation("Deleting all sessions for user {UserId}", userId);
        await using var ctx = await factory.CreateDbContextAsync(ct);
        await ctx.UserSessions.Where(s => s.UserId == userId).ExecuteDeleteAsync(ct);
        logger.LogInformation("Deleted all sessions for user {UserId}", userId);
        await ctx.SaveChangesAsync(ct);
    }
}
