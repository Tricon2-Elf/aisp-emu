using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AISpace.Portal;

namespace AISpace.Portal.Pages.Admin;

[Authorize(Policy = "PortalAdmin")]
public sealed class UsersModel(AuthPortalApiClient authApi, AreaPortalApiClient areaApi) : PageModel
{
    private const int PageSize = 50;

    public IReadOnlyList<UserRow> Users { get; private set; } = [];
    public string? Search { get; private set; }
    public int PageNumber { get; private set; }
    public bool HasNextPage { get; private set; }

    public async Task OnGetAsync(string? search, int? page, CancellationToken ct)
    {
        Search = search;
        PageNumber = Math.Max(page ?? 1, 1);
        var result = await authApi.GetUsersAsync(search, PageNumber, PageSize, ct);
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
        Users = result
            .Users.Select(user => new UserRow(
                user.UserId,
                user.Username,
                user.IsBanned,
                user.CharacterNames,
                roboCounts.GetValueOrDefault(user.UserId)
            ))
            .ToArray();
        HasNextPage = PageNumber * PageSize < result.Total;
    }

    public sealed record UserRow(
        int UserId,
        string Username,
        bool IsBanned,
        IReadOnlyList<string> CharacterNames,
        int RoboCount
    );
}
