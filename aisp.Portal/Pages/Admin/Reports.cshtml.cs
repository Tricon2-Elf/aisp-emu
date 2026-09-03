using aisp.Portal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace aisp.Portal.Pages.Admin;

[Authorize(Policy = "PortalAdmin")]
public sealed class ReportsModel(MsgPortalApiClient msgApi) : PageModel
{
    private const int DefaultPageSize = 50;

    public IReadOnlyList<PortalReportSummaryDto> Reports { get; private set; } = [];
    public string Status { get; private set; } = "Open";
    public int PageNumber { get; private set; } = 1;
    public int Total { get; private set; }
    public bool HasNextPage { get; private set; }

    public async Task OnGetAsync(string? status, int? pageNumber, CancellationToken ct)
    {
        Status = string.Equals(status, "Resolved", StringComparison.OrdinalIgnoreCase)
            ? "Resolved"
            : "Open";
        PageNumber = Math.Max(pageNumber ?? 1, 1);
        var result = await msgApi.GetReportsAsync(Status, PageNumber, DefaultPageSize, ct);
        Reports = result.Reports;
        Total = result.Total;
        HasNextPage = PageNumber * DefaultPageSize < result.Total;
    }
}
