using System.Security.Claims;
using aisp.Common.Game;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace aisp.Portal;

public static class PortalSignInHelper
{
    public static Task SignInAsync(HttpContext httpContext, PortalIdentityDto identity) =>
        httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    PortalAuthClaims.Create(identity),
                    CookieAuthenticationDefaults.AuthenticationScheme
                )
            )
        );
}
