using aisp.Common.Game;
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

    /// <summary>Empty = any; otherwise <c>online</c> or <c>offline</c>.</summary>
    public string OnlineFilter { get; private set; } = string.Empty;
    public int PageNumber { get; private set; }
    public string PageSize { get; private set; } = DefaultPageSize.ToString();
    public bool HasNextPage { get; private set; }

    public async Task OnGetAsync(
        string? search,
        int? pageNumber,
        string? pageSize,
        string? online,
        CancellationToken ct
    )
    {
        Search = search;
        OnlineFilter = NormalizeOnlineFilter(online);
        var showAll = string.Equals(pageSize, "all", StringComparison.OrdinalIgnoreCase);
        var selectedPageSize =
            int.TryParse(pageSize, out var parsedPageSize) && parsedPageSize is 20 or 50 or 100
                ? parsedPageSize
                : DefaultPageSize;
        if (!pageNumber.HasValue && int.TryParse(Request.Query["page"], out var legacyPageNumber))
            pageNumber = legacyPageNumber;
        PageSize = showAll ? "all" : selectedPageSize.ToString();
        PageNumber = showAll ? 1 : Math.Max(pageNumber ?? 1, 1);

        var filterByOnline = OnlineFilter switch
        {
            "online" => true,
            "offline" => false,
            _ => (bool?)null,
        };

        // Presence lives on Area. Filter against the full online-id set, then summarize only the page.
        var fetchAll = showAll || filterByOnline is not null;
        var result = await authApi.GetUsersAsync(
            search,
            PageNumber,
            selectedPageSize,
            fetchAll,
            ct
        );

        IReadOnlyList<PortalUserSummaryDto> pageUsers = result.Users;
        if (filterByOnline is { } wantOnline)
        {
            var onlineUserIds = (await areaApi.GetOnlineUserIdsAsync(ct)).ToHashSet();
            var filtered = result
                .Users.Where(user => onlineUserIds.Contains(user.UserId) == wantOnline)
                .ToArray();
            if (showAll)
            {
                pageUsers = filtered;
                HasNextPage = false;
            }
            else
            {
                pageUsers = filtered
                    .Skip((PageNumber - 1) * selectedPageSize)
                    .Take(selectedPageSize)
                    .ToArray();
                HasNextPage = PageNumber * selectedPageSize < filtered.Length;
            }
        }
        else
        {
            HasNextPage = !showAll && PageNumber * selectedPageSize < result.Total;
        }

        Users = await BuildRowsAsync(pageUsers, ct);
    }

    private async Task<IReadOnlyList<UserRow>> BuildRowsAsync(
        IReadOnlyList<PortalUserSummaryDto> users,
        CancellationToken ct
    )
    {
        if (users.Count == 0)
            return [];

        var summaries = await areaApi.GetSummariesAsync(
            users.Select(user => user.UserId).ToArray(),
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

        return users
            .Select(user => new UserRow(
                user.UserId,
                user.Username,
                user.Role,
                user.IsBanned,
                user.CharacterNames,
                roboCounts.GetValueOrDefault(user.UserId),
                onlineByUser.GetValueOrDefault(user.UserId),
                locationsByUser.GetValueOrDefault(user.UserId, "—"),
                user.CreatedAt,
                FormatAccountAge(user.CreatedAt)
            ))
            .ToArray();
    }

    private static string NormalizeOnlineFilter(string? online) =>
        online?.Trim().ToLowerInvariant() switch
        {
            "online" => "online",
            "offline" => "offline",
            _ => string.Empty,
        };

    internal static string FormatAccountAge(DateTime createdAtUtc)
    {
        var created = DateOnly.FromDateTime(createdAtUtc.ToUniversalTime());
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var totalDays = today.DayNumber - created.DayNumber;
        if (totalDays <= 0)
            return "Today";
        if (totalDays == 1)
            return "1 day";
        if (totalDays < 30)
            return $"{totalDays} days";

        var years = today.Year - created.Year;
        var months = today.Month - created.Month;
        if (today.Day < created.Day)
            months--;
        if (months < 0)
        {
            years--;
            months += 12;
        }

        if (years <= 0)
            return months == 1 ? "1 month" : $"{months} months";
        if (months == 0)
            return years == 1 ? "1 year" : $"{years} years";
        return years == 1 ? $"1 year {months}mo" : $"{years} years {months}mo";
    }

    public sealed record UserRow(
        int UserId,
        string Username,
        UserRole Role,
        bool IsBanned,
        IReadOnlyList<string> CharacterNames,
        int RoboCount,
        bool IsOnline,
        string Location,
        DateTime CreatedAt,
        string AccountAge
    );
}
