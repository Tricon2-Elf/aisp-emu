using System.Security.Claims;
using aisp.Portal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace aisp.Portal.Pages;

[Authorize]
public sealed class AccountModel(
    AuthPortalApiClient authApi,
    AreaPortalApiClient areaApi,
    MsgPortalApiClient msgApi
) : PageModel
{
    public PortalAccountDataDto Account { get; private set; } = default!;

    public string LanguageLabel =>
        LanguageOptions.FirstOrDefault(option => option.Value == Account.PreferredLanguage).Label
            is { Length: > 0 } label
            ? label
            : Account.PreferredLanguage;

    public IReadOnlyList<(string Value, string Label)> LanguageOptions { get; } =
    [("ja", "日本語"), ("en", "English"), ("zh-Hans", "简体中文"), ("zh-Hant", "繁體中文")];

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public bool StatusIsError { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Account = await areaApi.GetAccountAsync(userId, ct);
    }

    public async Task<IActionResult> OnPostChangePasswordAsync(
        string currentPassword,
        string newPassword,
        string confirmPassword,
        CancellationToken ct
    )
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try
        {
            await authApi.ChangePasswordAsync(
                userId,
                new PortalChangePasswordRequest(currentPassword, newPassword, confirmPassword),
                ct
            );
            await Task.WhenAll(
                authApi.DisconnectAsync(userId, ct),
                msgApi.DisconnectAsync(userId, ct),
                areaApi.DisconnectAsync(userId, ct)
            );
            StatusMessage = "Your password has been changed successfully.";
        }
        catch (PortalApiException exception)
        {
            StatusMessage = exception.Message;
            StatusIsError = true;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostChangeLanguageAsync(
        string preferredLanguage,
        CancellationToken ct
    )
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try
        {
            await areaApi.SetPreferredLanguageAsync(
                userId,
                new PortalChangeLanguageRequest(preferredLanguage),
                ct
            );
            StatusMessage =
                "Your game display language has been saved. Fully disconnect and log in again for it to take effect.";
        }
        catch (PortalApiException exception)
        {
            StatusMessage = exception.Message;
            StatusIsError = true;
        }

        return RedirectToPage();
    }
}
