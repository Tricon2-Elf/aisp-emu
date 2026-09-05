using aisp.Portal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace aisp.Portal.Pages.Admin;

[Authorize(Policy = "PortalAdmin")]
public sealed class ReportDetailModel(MsgPortalApiClient msgApi) : PageModel
{
    public PortalReportDetailDto Report { get; private set; } = default!;
    public int ActorUserId { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public bool StatusIsError { get; set; }

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken ct)
    {
        try
        {
            Report = await msgApi.GetReportAsync(id, ct);
            ActorUserId = PortalAuthClaims.GetUserId(User);
            return Page();
        }
        catch (PortalApiException)
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> OnPostResolveAsync(long id, string action, CancellationToken ct)
    {
        ActorUserId = PortalAuthClaims.GetUserId(User);
        if (string.IsNullOrWhiteSpace(action))
        {
            StatusMessage = "Please describe the action taken before resolving.";
            StatusIsError = true;
            return RedirectToPage(new { id });
        }

        try
        {
            await msgApi.ResolveReportAsync(id, ActorUserId, action.Trim(), ct);
            StatusMessage = "Report marked as resolved.";
            return RedirectToPage(new { id });
        }
        catch (PortalApiException exception)
        {
            StatusMessage = exception.Message;
            StatusIsError = true;
            return RedirectToPage(new { id });
        }
    }
}
