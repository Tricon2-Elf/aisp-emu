using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using AISpace.Portal;

namespace AISpace.Portal.Pages;

public sealed class LoginModel(
    AuthPortalApiClient authApi,
    IOptions<AdminPortalOptions> adminOptions,
    IOptions<UserPortalOptions> userPortalOptions
) : PageModel
{
    [BindProperty]
    public LoginInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public bool UserPortalEnabled => userPortalOptions.Value.Enabled;

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var identity = await authApi.LoginAsync(new(Input.Username, Input.Password), ct);
            await SignInAsync(identity.UserId, identity.Username);
            var fallbackUrl = UserPortalEnabled
                ? "/account"
                : adminOptions.Value.IsAdmin(identity.Username)
                    ? "/admin/users"
                    : "/login";
            return LocalRedirect(IsLocalReturnUrl(ReturnUrl) ? ReturnUrl! : fallbackUrl);
        }
        catch (PortalApiException)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return Page();
        }
    }

    private async Task SignInAsync(int userId, string username)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username),
        };
        if (adminOptions.Value.Enabled && adminOptions.Value.IsAdmin(username))
            claims.Add(new("portal_admin", "true"));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }

    private bool IsLocalReturnUrl(string? returnUrl) => !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl);

    public sealed class LoginInput
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
