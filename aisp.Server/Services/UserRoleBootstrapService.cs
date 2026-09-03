using aisp.Common.DAL.Repositories;
using aisp.Common.Game;

namespace aisp.Server.Services;

public static class UserRoleBootstrapService
{
    public static async Task PromoteIfListedAsync(
        IUserRepository users,
        string username,
        IReadOnlyList<string> adminUsernames,
        CancellationToken ct = default
    )
    {
        if (
            !adminUsernames.Any(candidate =>
                string.Equals(candidate, username, StringComparison.Ordinal)
            )
        )
            return;

        var user = await users.GetByUsernameAsync(username);
        if (user is null || user.Role >= UserRole.ServerAdmin)
            return;

        await users.PromoteToServerAdminIfBelowAsync(user.Id, ct);
    }

    public static async Task PromoteAllListedAsync(
        IUserRepository users,
        IReadOnlyList<string> adminUsernames,
        CancellationToken ct = default
    )
    {
        foreach (var username in adminUsernames)
            await PromoteIfListedAsync(users, username, adminUsernames, ct);
    }
}
