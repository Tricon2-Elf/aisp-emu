using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using AISpace.Portal;

namespace AISpace.Portal.Pages.Admin;

[Authorize(Policy = "PortalAdmin")]
public sealed class UserDetailModel(
    AuthPortalApiClient authApi,
    AreaPortalApiClient areaApi,
    MsgPortalApiClient msgApi,
    ILogger<UserDetailModel> logger
) : PageModel
{
    public PortalUserDetailDto TargetUser { get; private set; } = default!;
    public PortalAccountDataDto Account { get; private set; } = default!;

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
        try
        {
            await authApi.SetPasswordAsync(
                userId,
                new PortalSetPasswordRequest(newPassword, confirmPassword),
                ct
            );
            await DisconnectEverywhereAsync(userId, ct);
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
        string? reason,
        CancellationToken ct
    )
    {
        if (!CanModerate(userId))
            return Forbid();
        if (reason?.Length > 256)
            return BadRequest();
        reason = NormalizeReason(reason);
        await DisconnectEverywhereAsync(userId, ct);
        logger.LogInformation(
            "Portal moderator {Moderator} kicked user {UserId}. Reason: {Reason}",
            User.Identity?.Name,
            userId,
            reason ?? "No reason provided"
        );
        return RedirectToPage(new { userId });
    }

    public async Task<IActionResult> OnPostBanAsync(
        int userId,
        string? reason,
        CancellationToken ct
    )
    {
        if (!CanModerate(userId))
            return Forbid();
        if (reason?.Length > 256)
            return BadRequest();
        reason = NormalizeReason(reason);
        await authApi.BanAsync(userId, reason, ct);
        await DisconnectEverywhereAsync(userId, ct);
        return RedirectToPage(new { userId });
    }

    public async Task<IActionResult> OnPostUnbanAsync(int userId, CancellationToken ct)
    {
        if (!CanModerate(userId))
            return Forbid();
        await authApi.UnbanAsync(userId, ct);
        return RedirectToPage(new { userId });
    }

    private async Task LoadAsync(int userId, CancellationToken ct)
    {
        TargetUser = await authApi.GetUserAsync(userId, ct);
        Account = await areaApi.GetAccountAsync(userId, ct);
    }

    private bool CanModerate(int targetUserId) =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!) != targetUserId;

    private static string? NormalizeReason(string? reason) =>
        string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

    private async Task DisconnectEverywhereAsync(int userId, CancellationToken ct)
    {
        await Task.WhenAll(
            authApi.DisconnectAsync(userId, ct),
            msgApi.DisconnectAsync(userId, ct),
            areaApi.DisconnectAsync(userId, ct)
        );
    }
}
