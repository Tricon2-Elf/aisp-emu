namespace aisp.Common.Game;

public enum UserRole : byte
{
    User = 0,
    Moderator = 1,
    Admin = 2,
    ServerAdmin = 3,
}

public static class UserRoleExtensions
{
    public static bool HasPortalAccess(this UserRole role) => role >= UserRole.Moderator;

    public static bool CanKickOrBan(this UserRole role) => role >= UserRole.Moderator;

    public static bool CanAssignModerator(this UserRole role) => role >= UserRole.Admin;

    public static bool CanSetRole(this UserRole actorRole, UserRole targetRole, UserRole newRole)
    {
        if (actorRole < UserRole.Admin)
            return false;
        if (actorRole == UserRole.Admin)
        {
            if (targetRole >= UserRole.Admin || newRole >= UserRole.Admin)
                return false;
        }
        else if (actorRole == UserRole.ServerAdmin)
        {
            if (targetRole >= UserRole.ServerAdmin || newRole >= UserRole.ServerAdmin)
                return false;
        }

        return true;
    }

    public static bool CanActOn(this UserRole actorRole, UserRole targetRole) =>
        actorRole > targetRole;
}
