using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using aisp.Common.Game;
using Microsoft.Extensions.DependencyInjection;

namespace aisp.Portal;

public static class PortalHttpClientNames
{
    public const string Auth = "PortalAuthApi";
    public const string Msg = "PortalMsgApi";
    public const string Area = "PortalAreaApi";
}

public sealed class PortalApiException(HttpStatusCode statusCode, string message)
    : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

public sealed class AuthPortalApiClient(
    [FromKeyedServices(PortalHttpClientNames.Auth)] HttpClient httpClient
)
{
    public async Task<PortalIdentityDto> RegisterAsync(
        RegisterPortalAccountRequest request,
        CancellationToken ct
    ) =>
        await PostAsync<RegisterPortalAccountRequest, PortalIdentityDto>(
            "api/auth/portal/register",
            request,
            ct
        );

    public async Task<PortalIdentityDto> LoginAsync(
        PortalLoginRequest request,
        CancellationToken ct
    ) =>
        await PostAsync<PortalLoginRequest, PortalIdentityDto>(
            "api/auth/portal/session",
            request,
            ct
        );

    public async Task<PortalUserPageDto> GetUsersAsync(
        string? search,
        int page,
        int pageSize,
        bool all,
        CancellationToken ct
    )
    {
        var query = all
            ? "api/auth/portal/users?all=true"
            : $"api/auth/portal/users?skip={(page - 1) * pageSize}&take={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            query += $"&search={Uri.EscapeDataString(search)}";
        return await GetAsync<PortalUserPageDto>(query, ct);
    }

    public Task<PortalUserDetailDto> GetUserAsync(int userId, CancellationToken ct) =>
        GetAsync<PortalUserDetailDto>($"api/auth/portal/users/{userId}", ct);

    public Task BanAsync(
        int userId,
        int actorUserId,
        int? days,
        string? reason,
        CancellationToken ct
    ) =>
        PostNoContentAsync(
            $"api/auth/portal/users/{userId}/ban",
            new PortalBanRequest(actorUserId, days, reason),
            ct
        );

    public Task KickAsync(
        int userId,
        int actorUserId,
        int? minutes,
        string? reason,
        CancellationToken ct
    ) =>
        PostNoContentAsync(
            $"api/auth/portal/users/{userId}/kick",
            new PortalKickRequest(actorUserId, minutes, reason),
            ct
        );

    public Task UnbanAsync(int userId, int actorUserId, CancellationToken ct) =>
        PostNoContentAsync(
            $"api/auth/portal/users/{userId}/unban",
            new PortalActorRequest(actorUserId),
            ct
        );

    public Task SetRoleAsync(int userId, int actorUserId, UserRole role, CancellationToken ct) =>
        PostNoContentAsync(
            $"api/auth/portal/users/{userId}/role",
            new PortalSetRoleRequest(actorUserId, role),
            ct
        );

    public Task SetPasswordAsync(
        int userId,
        PortalSetPasswordRequest request,
        CancellationToken ct
    ) => PostNoContentAsync($"api/auth/portal/users/{userId}/password", request, ct);

    public Task ChangePasswordAsync(
        int userId,
        PortalChangePasswordRequest request,
        CancellationToken ct
    ) => PostNoContentAsync($"api/auth/portal/users/{userId}/password/change", request, ct);

    public Task<PortalDisconnectResultDto> DisconnectAsync(int userId, CancellationToken ct) =>
        PostAsync<object?, PortalDisconnectResultDto>(
            $"api/auth/portal/users/{userId}/disconnect",
            null,
            ct
        );

    private async Task<TResponse> GetAsync<TResponse>(string path, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(path, ct);
        return await ReadAsync<TResponse>(response, ct);
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(
        string path,
        TRequest request,
        CancellationToken ct
    )
    {
        using var response = await httpClient.PostAsJsonAsync(path, request, ct);
        return await ReadAsync<TResponse>(response, ct);
    }

    private async Task PostNoContentAsync<TRequest>(
        string path,
        TRequest request,
        CancellationToken ct
    )
    {
        using var response = await httpClient.PostAsJsonAsync(path, request, ct);
        if (!response.IsSuccessStatusCode)
            throw await ToExceptionAsync(response, ct);
    }

    private static async Task<TResponse> ReadAsync<TResponse>(
        HttpResponseMessage response,
        CancellationToken ct
    )
    {
        if (!response.IsSuccessStatusCode)
            throw await ToExceptionAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct)
            ?? throw new PortalApiException(
                response.StatusCode,
                "The backend returned an empty response."
            );
    }

    private static async Task<PortalApiException> ToExceptionAsync(
        HttpResponseMessage response,
        CancellationToken ct
    )
    {
        return await AuthPortalApiClientError.ToExceptionAsync(response, ct);
    }
}

public sealed class AreaPortalApiClient(
    [FromKeyedServices(PortalHttpClientNames.Area)] HttpClient httpClient
)
{
    public async Task<PortalAccountDataDto> GetAccountAsync(int userId, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(
            $"api/area/portal/users/{userId}/account",
            ct
        );
        return await ReadAsync<PortalAccountDataDto>(response, ct);
    }

    public async Task SetPreferredLanguageAsync(
        int userId,
        PortalChangeLanguageRequest request,
        CancellationToken ct
    )
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"api/area/portal/users/{userId}/language",
            request,
            ct
        );
        if (!response.IsSuccessStatusCode)
            throw await AuthPortalApiClientError.ToExceptionAsync(response, ct);
    }

    public async Task ResetRoboAsync(
        int userId,
        int characterId,
        uint roboId,
        PortalResetRoboRequest request,
        CancellationToken ct
    )
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"api/area/portal/users/{userId}/characters/{characterId}/robos/{roboId}/reset",
            request,
            ct
        );
        if (!response.IsSuccessStatusCode)
            throw await AuthPortalApiClientError.ToExceptionAsync(response, ct);
    }

    public async Task<IReadOnlyList<PortalCharacterRoboSummaryDto>> GetSummariesAsync(
        IReadOnlyList<int> userIds,
        CancellationToken ct
    )
    {
        var summaries = new List<PortalCharacterRoboSummaryDto>(userIds.Count);
        foreach (var batch in userIds.Distinct().Chunk(100))
        {
            using var response = await httpClient.PostAsJsonAsync(
                "api/area/portal/users/summaries",
                new PortalUserIdsRequest(batch),
                ct
            );
            summaries.AddRange(
                await ReadAsync<IReadOnlyList<PortalCharacterRoboSummaryDto>>(response, ct)
            );
        }
        return summaries;
    }

    public async Task<PortalDisconnectResultDto> DisconnectAsync(int userId, CancellationToken ct)
    {
        using var response = await httpClient.PostAsJsonAsync<object?>(
            $"api/area/portal/users/{userId}/disconnect",
            null,
            ct
        );
        return await ReadAsync<PortalDisconnectResultDto>(response, ct);
    }

    private static async Task<TResponse> ReadAsync<TResponse>(
        HttpResponseMessage response,
        CancellationToken ct
    )
    {
        if (!response.IsSuccessStatusCode)
            throw await AuthPortalApiClientError.ToExceptionAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct)
            ?? throw new PortalApiException(
                response.StatusCode,
                "The backend returned an empty response."
            );
    }
}

public sealed class MsgPortalApiClient(
    [FromKeyedServices(PortalHttpClientNames.Msg)] HttpClient httpClient
)
{
    public async Task<PortalDisconnectResultDto> DisconnectAsync(int userId, CancellationToken ct)
    {
        using var response = await httpClient.PostAsJsonAsync<object?>(
            $"api/msg/portal/users/{userId}/disconnect",
            null,
            ct
        );
        if (!response.IsSuccessStatusCode)
            throw await AuthPortalApiClientError.ToExceptionAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<PortalDisconnectResultDto>(
                cancellationToken: ct
            )
            ?? throw new PortalApiException(
                response.StatusCode,
                "The backend returned an empty response."
            );
    }

    public async Task<PortalChatPageDto> GetUserChatAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        var skip = Math.Max(page - 1, 0) * pageSize;
        using var response = await httpClient.GetAsync(
            $"api/msg/portal/users/{userId}/chat?skip={skip}&take={pageSize}",
            ct
        );
        if (!response.IsSuccessStatusCode)
            throw await AuthPortalApiClientError.ToExceptionAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<PortalChatPageDto>(cancellationToken: ct)
            ?? throw new PortalApiException(
                response.StatusCode,
                "The backend returned an empty response."
            );
    }
}

internal static class AuthPortalApiClientError
{
    internal static async Task<PortalApiException> ToExceptionAsync(
        HttpResponseMessage response,
        CancellationToken ct
    )
    {
        var content = await response.Content.ReadAsStringAsync(ct);
        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                var error = JsonSerializer.Deserialize<PortalErrorDto>(
                    content,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web)
                );
                if (!string.IsNullOrWhiteSpace(error?.Error))
                    return new PortalApiException(response.StatusCode, error.Error);
            }
            catch (JsonException)
            {
                // Non-JSON error bodies are intentionally reduced to the safe generic message below.
            }
        }

        return new PortalApiException(response.StatusCode, "The backend request failed.");
    }
}
