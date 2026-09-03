using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;

namespace aisp.Common.Game;

public static class UserModerationState
{
    public static bool IsCurrentlyBanned(User user, DateTime? utcNow = null)
    {
        utcNow ??= DateTime.UtcNow;
        if (!user.IsBanned)
            return false;

        return user.BannedUntil is null || user.BannedUntil > utcNow;
    }

    public static bool IsCurrentlyKicked(User user, DateTime? utcNow = null)
    {
        utcNow ??= DateTime.UtcNow;
        return user.KickedUntil is not null && user.KickedUntil > utcNow;
    }

    public static bool ShouldClearExpiredBan(User user, DateTime? utcNow = null)
    {
        utcNow ??= DateTime.UtcNow;
        return user.IsBanned
            && user.BannedUntil is not null
            && user.BannedUntil <= utcNow;
    }

    public static async Task<User?> PrepareUserForGameLoginAsync(
        IUserRepository userRepo,
        int userId,
        CancellationToken ct = default
    )
    {
        await userRepo.ClearExpiredBanAsync(userId, ct);
        return await userRepo.GetById(userId);
    }
}
