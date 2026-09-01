using System.Security.Claims;
using aisp.Common.Game;
using aisp.Common.Services;
using aisp.Portal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace aisp.Portal.Pages.Admin;

[Authorize(Policy = "PortalAdmin")]
public sealed class UserDetailModel(
    AuthPortalApiClient authApi,
    MsgPortalApiClient msgApi,
    AreaPortalApiClient areaApi,
    ILogger<UserDetailModel> logger
) : PageModel
{
    public PortalUserDetailDto TargetUser { get; private set; } = default!;
    public PortalAccountDataDto Account { get; private set; } = default!;
    public UserRole ActorRole { get; private set; }
    public int ActorUserId { get; private set; }
    public bool CanModerateTargetUser { get; private set; }
    public bool CanChangeRole { get; private set; }
    public IReadOnlyList<UserRole> AssignableRoles { get; private set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public bool StatusIsError { get; set; }

    public async Task OnGetAsync(int userId, CancellationToken ct) => await LoadAsync(userId, ct);

    public async Task<IActionResult> OnPostSetPasswordAsync(
        int userId,
        string newPassword,
        string confirmPassword,
        CancellationToken ct
    )
    {
        await LoadAsync(userId, ct);
        if (!CanModerateTargetUser)
            return Forbid();
        try
        {
            await authApi.SetPasswordAsync(
                userId,
                new PortalSetPasswordRequest(ActorUserId, newPassword, confirmPassword),
                ct
            );
            await authApi.DisconnectAsync(userId, ct);
            await msgApi.DisconnectAsync(userId, ct);
            await areaApi.DisconnectAsync(userId, ct);
            logger.LogInformation(
                "Portal moderator {Moderator} reset the password for user {UserId}",
                User.Identity?.Name,
                userId
            );
            StatusMessage =
                "Password reset successfully. The user has been disconnected from all game servers.";
        }
        catch (PortalApiException exception)
        {
            StatusMessage = exception.Message;
            StatusIsError = true;
        }

        return RedirectToPage(new { userId });
    }

    public async Task<IActionResult> OnPostKickAsync(
        int userId,
        int? minutes,
        string? reason,
        CancellationToken ct
    )
    {
        await LoadAsync(userId, ct);
        if (!CanModerateTargetUser)
            return Forbid();
        if (reason?.Length > 256)
            return BadRequest();
        reason = NormalizeReason(reason);
        try
        {
            await authApi.KickAsync(
                userId,
                ActorUserId,
                minutes ?? ModerationService.DefaultKickMinutes,
                reason,
                ct
            );
            logger.LogInformation(
                "Portal moderator {Moderator} kicked user {UserId}. Reason: {Reason}",
                User.Identity?.Name,
                userId,
                reason ?? "No reason provided"
            );
        }
        catch (PortalApiException exception)
        {
            StatusMessage = exception.Message;
            StatusIsError = true;
            return RedirectToPage(new { userId });
        }

        return RedirectToPage(new { userId });
    }

    public async Task<IActionResult> OnPostBanAsync(
        int userId,
        int? days,
        string? reason,
        CancellationToken ct
    )
    {
        await LoadAsync(userId, ct);
        if (!CanModerateTargetUser)
            return Forbid();
        if (reason?.Length > 256)
            return BadRequest();
        reason = NormalizeReason(reason);
        try
        {
            await authApi.BanAsync(
                userId,
                ActorUserId,
                days ?? ModerationService.DefaultBanDays,
                reason,
                ct
            );
        }
        catch (PortalApiException exception)
        {
            StatusMessage = exception.Message;
            StatusIsError = true;
            return RedirectToPage(new { userId });
        }

        return RedirectToPage(new { userId });
    }

    public async Task<IActionResult> OnPostUnbanAsync(int userId, CancellationToken ct)
    {
        await LoadAsync(userId, ct);
        if (!CanModerateTargetUser)
            return Forbid();
        try
        {
            await authApi.UnbanAsync(userId, ActorUserId, ct);
        }
        catch (PortalApiException exception)
        {
            StatusMessage = exception.Message;
            StatusIsError = true;
            return RedirectToPage(new { userId });
        }

        return RedirectToPage(new { userId });
    }

    public async Task<IActionResult> OnPostSetRoleAsync(
        int userId,
        UserRole role,
        CancellationToken ct
    )
    {
        await LoadAsync(userId, ct);
        if (!CanModerateTargetUser || !ActorRole.CanSetRole(TargetUser.Role, role))
            return Forbid();
        try
        {
            await authApi.SetRoleAsync(userId, ActorUserId, role, ct);
            StatusMessage = $"Role updated to {role}.";
        }
        catch (PortalApiException exception)
        {
            StatusMessage = exception.Message;
            StatusIsError = true;
        }

        return RedirectToPage(new { userId });
    }

    private async Task LoadAsync(int userId, CancellationToken ct)
    {
        ActorUserId = PortalAuthClaims.GetUserId(User);
        var actorUser = await authApi.GetUserAsync(ActorUserId, ct);
        ActorRole = actorUser.Role;
        TargetUser = await authApi.GetUserAsync(userId, ct);
        Account = await areaApi.GetAccountAsync(userId, ct);
        CanModerateTargetUser = PortalAuthClaims.CanModerate(
            ActorRole,
            ActorUserId,
            TargetUser.UserId,
            TargetUser.Role
        );
        CanChangeRole = CanModerateTargetUser && ActorRole >= UserRole.Admin;
        AssignableRoles = BuildAssignableRoles(ActorRole, TargetUser.Role);
    }

    private static IReadOnlyList<UserRole> BuildAssignableRoles(
        UserRole actorRole,
        UserRole targetRole
    )
    {
        if (actorRole < UserRole.Admin || !actorRole.CanActOn(targetRole))
            return [];

        IEnumerable<UserRole> roles = actorRole switch
        {
            UserRole.Admin => [UserRole.User, UserRole.Moderator],
            UserRole.ServerAdmin => Enum.GetValues<UserRole>()
                .Where(role => role < UserRole.ServerAdmin),
            _ => [],
        };

        return roles.Where(role => actorRole.CanSetRole(targetRole, role)).ToArray();
    }

    private static string? NormalizeReason(string? reason) =>
        string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
}
