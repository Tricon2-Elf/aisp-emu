using aisp.Portal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace aisp.Portal.Pages.Admin;

[Authorize(Policy = "PortalAdmin")]
public sealed class UsersModel(AuthPortalApiClient authApi, AreaPortalApiClient areaApi) : PageModel
{
    private const int DefaultPageSize = 50;

    public IReadOnlyList<UserRow> Users { get; private set; } = [];
    public string? Search { get; private set; }
    public int PageNumber { get; private set; }
    public string PageSize { get; private set; } = DefaultPageSize.ToString();
    public bool HasNextPage { get; private set; }

    public async Task OnGetAsync(
        string? search,
        int? pageNumber,
        string? pageSize,
        CancellationToken ct
    )
    {
        Search = search;
        var showAll = string.Equals(pageSize, "all", StringComparison.OrdinalIgnoreCase);
        var selectedPageSize =
            int.TryParse(pageSize, out var parsedPageSize) && parsedPageSize is 20 or 50 or 100
                ? parsedPageSize
                : DefaultPageSize;
        if (!pageNumber.HasValue && int.TryParse(Request.Query["page"], out var legacyPageNumber))
            pageNumber = legacyPageNumber;
        PageSize = showAll ? "all" : selectedPageSize.ToString();
        PageNumber = showAll ? 1 : Math.Max(pageNumber ?? 1, 1);
        var result = await authApi.GetUsersAsync(search, PageNumber, selectedPageSize, showAll, ct);
        var summaries =
            result.Users.Count == 0
                ? []
                : await areaApi.GetSummariesAsync(
                    result.Users.Select(user => user.UserId).ToArray(),
                    ct
                );
        var roboCounts = summaries.ToDictionary(
            summary => summary.UserId,
            summary => summary.Characters.Sum(character => character.RoboCount)
        );
        var onlineByUser = summaries.ToDictionary(
            summary => summary.UserId,
            summary => summary.Characters.Any(character => character.IsOnline)
        );
        var locationsByUser = summaries.ToDictionary(
            summary => summary.UserId,
            summary =>
            {
                var locations = summary
                    .Characters.Where(character => character.IsOnline)
                    .Select(character => character.Location)
                    .Distinct()
                    .ToArray();
                return locations.Length == 0 ? "—" : string.Join(", ", locations);
            }
        );
        Users = result
            .Users.Select(user => new UserRow(
                user.UserId,
                user.Username,
                user.IsBanned,
                user.CharacterNames,
                roboCounts.GetValueOrDefault(user.UserId),
                onlineByUser.GetValueOrDefault(user.UserId),
                locationsByUser.GetValueOrDefault(user.UserId, "—")
            ))
            .ToArray();
        HasNextPage = !showAll && PageNumber * selectedPageSize < result.Total;
    }

    public sealed record UserRow(
        int UserId,
        string Username,
        bool IsBanned,
        IReadOnlyList<string> CharacterNames,
        int RoboCount,
        bool IsOnline,
        string Location
    );
}
