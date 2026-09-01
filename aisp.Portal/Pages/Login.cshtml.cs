using System.ComponentModel.DataAnnotations;
using aisp.Common.Game;
using aisp.Portal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace aisp.Portal.Pages;

public sealed class LoginModel(AuthPortalApiClient authApi, IOptions<PortalOptions> portalOptions)
    : PageModel
{
    [BindProperty]
    public LoginInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public bool AllowRegistration => portalOptions.Value.AllowRegistration;

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var identity = await authApi.LoginAsync(new(Input.Username, Input.Password), ct);
            await PortalSignInHelper.SignInAsync(HttpContext, identity);
            var fallbackUrl = identity.Role.HasPortalAccess() ? "/admin/users" : "/account";
            return LocalRedirect(IsLocalReturnUrl(ReturnUrl) ? ReturnUrl! : fallbackUrl);
        }
        catch (PortalApiException)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return Page();
        }
    }

    private bool IsLocalReturnUrl(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl);

    public sealed class LoginInput
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
