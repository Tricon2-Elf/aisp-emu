using System.Security.Claims;
using AISpace.BackendApi.Contracts;
using AISpace.Portal.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AISpace.AdminPortal.Pages.Admin;

[Authorize(Policy = "PortalAdmin")]
public sealed class UserDetailModel(
    AuthPortalApiClient authApi,
    AreaPortalApiClient areaApi,
    MsgPortalApiClient msgApi
) : PageModel
{
    public PortalUserDetailDto TargetUser { get; private set; } = default!;
    public PortalAccountDataDto Account { get; private set; } = default!;

    public async Task OnGetAsync(int userId, CancellationToken ct) => await LoadAsync(userId, ct);

    public async Task<IActionResult> OnPostKickAsync(int userId, CancellationToken ct)
    {
        if (!CanModerate(userId))
            return Forbid();
        await DisconnectEverywhereAsync(userId, ct);
        return RedirectToPage(new { userId });
    }

    public async Task<IActionResult> OnPostBanAsync(int userId, string? reason, CancellationToken ct)
    {
        if (!CanModerate(userId))
            return Forbid();
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

    private bool CanModerate(int targetUserId) => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!) != targetUserId;

    private async Task DisconnectEverywhereAsync(int userId, CancellationToken ct)
    {
        await Task.WhenAll(authApi.DisconnectAsync(userId, ct), msgApi.DisconnectAsync(userId, ct), areaApi.DisconnectAsync(userId, ct));
    }
}
