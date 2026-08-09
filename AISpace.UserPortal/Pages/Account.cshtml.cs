using System.Security.Claims;
using AISpace.BackendApi.Contracts;
using AISpace.Portal.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AISpace.UserPortal.Pages;

[Authorize]
public sealed class AccountModel(AreaPortalApiClient areaApi) : PageModel
{
    public PortalAccountDataDto Account { get; private set; } = default!;

    public async Task OnGetAsync(CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Account = await areaApi.GetAccountAsync(userId, ct);
    }
}
