using aisp.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace aisp.Common.DAL.Repositories;

public interface IUserSessionRepository
{
    Task<UserSession> CreateAsync(
        int userId,
        string otp,
        TimeSpan duration,
        CancellationToken ct = default
    );
    Task<UserSession?> GetValidSessionAsync(string otp, CancellationToken ct = default);
    Task InvalidateExpiredAsync(CancellationToken ct = default);
    Task DeleteAllForUserAsync(int userId, CancellationToken ct = default);
}

public class UserSessionRepository(MainContext db, ILogger<UserSessionRepository> logger)
    : IUserSessionRepository
{
    public async Task<UserSession> CreateAsync(
        int userId,
        string otp,
        TimeSpan duration,
        CancellationToken ct = default
    )
    {
        var session = new UserSession
        {
            UserId = userId,
            OTP = otp,
            ExpiresAt = DateTime.UtcNow.Add(duration),
        };

        db.UserSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return session;
    }

    public async Task<UserSession?> GetValidSessionAsync(string otp, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await db
            .UserSessions.Include(s => s.User)
                .ThenInclude(u => u.Characters)
                    .ThenInclude(c => c.Equipment)
            .Where(s => s.OTP == otp && s.ExpiresAt > now)
            .SingleOrDefaultAsync(ct);
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
        await db.UserSessions.Where(s => s.UserId == userId).ExecuteDeleteAsync(ct);
        logger.LogInformation("Deleted all sessions for user {UserId}", userId);
    }
}
