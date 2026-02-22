using AISpace.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.DAL.Repositories;

public class UserSessionRepository(MainContext db) : IUserSessionRepository
{
    public async Task<UserSession> CreateAsync(int userId, string otp, TimeSpan duration, CancellationToken ct = default)
    {
        var session = new UserSession
        {
            UserId = userId,
            OTP = otp,
            ExpiresAt = DateTime.UtcNow.Add(duration)
        };
        db.UserSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return session;
    }

    public async Task<UserSession?> GetValidSessionAsync(string otp, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await db.UserSessions
            .Include(s => s.User)
                .ThenInclude(u => u.Characters)
                    .ThenInclude(c => c.Equipment)
            .Where(s => s.OTP == otp && s.ExpiresAt > now)
            .SingleOrDefaultAsync(ct);
    }

    public async Task InvalidateExpiredAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var expired = await db.UserSessions.Where(s => s.ExpiresAt <= now).ToListAsync(ct);
        if (expired.Count > 0)
        {
            db.UserSessions.RemoveRange(expired);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task DeleteAllForUserAsync(int userId, CancellationToken ct = default)
    {
        var sessions = await db.UserSessions.Where(s => s.UserId == userId).ToListAsync(ct);
        if (sessions.Count > 0)
        {
            db.UserSessions.RemoveRange(sessions);
            await db.SaveChangesAsync(ct);
        }
    }
}