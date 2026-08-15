using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using aisp.Portal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace aisp.Portal.Pages;

public sealed class RegisterModel(
    AuthPortalApiClient authApi,
    IOptions<PortalOptions> portalOptions
) : PageModel
{
    [BindProperty]
    public RegisterInput Input { get; set; } = new();

    public IActionResult OnGet() =>
        portalOptions.Value.AllowRegistration ? Page() : RedirectToPage("/Login");

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!portalOptions.Value.AllowRegistration)
            return RedirectToPage("/Login");

        if (!ModelState.IsValid)
            return Page();

        try
        {
            var identity = await authApi.RegisterAsync(
                new(Input.Username, Input.Password, Input.ConfirmPassword),
                ct
            );
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, identity.UserId.ToString()),
                new(ClaimTypes.Name, identity.Username),
            };
            if (portalOptions.Value.IsAdmin(identity.Username))
                claims.Add(new("portal_admin", "true"));
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(
                    new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)
                )
            );
            return Redirect("/account");
        }
        catch (PortalApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }

    public sealed class RegisterInput
    {
        [
            Required,
            RegularExpression(
                "^[A-Za-z0-9_.-]{3,64}$",
                ErrorMessage = "Username must be 3–64 characters and use only letters, numbers, underscores, dots, or hyphens."
            )
        ]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [
            Required,
            StringLength(
                128,
                MinimumLength = 8,
                ErrorMessage = "Password must be at least 8 characters."
            )
        ]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
