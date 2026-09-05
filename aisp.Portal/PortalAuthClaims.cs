using System.Security.Claims;
using aisp.Common.Game;

namespace aisp.Portal;

public static class PortalAuthClaims
{
    public const string PortalAdmin = "portal_admin";
    public const string PortalRole = "portal_role";

    public static List<Claim> Create(PortalIdentityDto identity)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, identity.UserId.ToString()),
            new(ClaimTypes.Name, identity.Username),
            new(PortalRole, ((byte)identity.Role).ToString()),
        };
        if (identity.Role.HasPortalAccess())
            claims.Add(new(PortalAdmin, "true"));
        return claims;
    }

    public static List<Claim> Create(PortalUserDetailDto user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(PortalRole, ((byte)user.Role).ToString()),
        };
        if (user.Role.HasPortalAccess())
            claims.Add(new(PortalAdmin, "true"));
        return claims;
    }

    public static int GetUserId(ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static UserRole GetRole(ClaimsPrincipal user)
    {
        var roleText = user.FindFirstValue(PortalRole);
        if (string.IsNullOrEmpty(roleText))
            return UserRole.User;

        if (byte.TryParse(roleText, out var roleByte) && Enum.IsDefined(typeof(UserRole), roleByte))
            return (UserRole)roleByte;

        return Enum.TryParse<UserRole>(roleText, ignoreCase: true, out var role)
            ? role
            : UserRole.User;
    }

    public static bool CanModerate(
        UserRole actorRole,
        int actorUserId,
        int targetUserId,
        UserRole targetRole
    ) =>
        actorUserId != targetUserId
        && actorRole.CanKickOrBan()
        && actorRole.CanActOn(targetRole);
}
