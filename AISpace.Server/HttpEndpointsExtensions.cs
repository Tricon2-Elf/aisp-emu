using AISpace.Common;
using AISpace.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AISpace.Server;

internal static class HttpEndpointsExtensions
{
    internal static WebApplication MapAispaceHttpEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapGet(
            "/healthz",
            (GameServerHealthRegistry registry) =>
            {
                var servers = registry.GetSnapshot();
                var allHealthy = servers.Values.All(s => s.State == "healthy");
                return Results.Json(new { status = allHealthy ? "Healthy" : "Unhealthy", servers }, statusCode: allHealthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
            }
        );

        app.MapPost("/api/broadcast", (HttpRequest request, BroadcastService broadcast, ILoggerFactory loggerFactory) => HandleBroadcastAsync(null, request, broadcast, loggerFactory));
        app.MapPost("/api/area/broadcast", (HttpRequest request, BroadcastService broadcast, ILoggerFactory loggerFactory) => HandleBroadcastAsync("area", request, broadcast, loggerFactory));
        app.MapPost("/api/msg/broadcast", (HttpRequest request, BroadcastService broadcast, ILoggerFactory loggerFactory) => HandleBroadcastAsync("msg", request, broadcast, loggerFactory));

        app.MapPost(
            "/api/users",
            async (HttpRequest request, UserAdminService service, CancellationToken ct) =>
            {
                var body = await request.ReadFromJsonAsync<CreateUserRequest>(ct);
                if (body == null || string.IsNullOrWhiteSpace(body.Username))
                    return Results.BadRequest(new { error = "username is required" });

                var (success, error, user) = await service.CreateUserAsync(body.Username, body.Password, ct);
                if (!success || user == null)
                    return Results.BadRequest<object>(new { error = "failed to create user" });

                return Results.Ok(
                    new
                    {
                        user.Id,
                        user.Username,
                        user.CreatedAt,
                    }
                );
            }
        );

        app.MapDelete(
            "/api/users/{username}",
            async (string username, UserAdminService service, CancellationToken ct) =>
            {
                var (success, error) = await service.DeleteUserAsync(username, ct);
                if (!success)
                    return Results.NotFound(new { error });

                return Results.Ok(new { deleted = true, username });
            }
        );

        app.MapPost(
            "/api/users/{username}/reset-password",
            async (string username, HttpRequest request, UserAdminService service, CancellationToken ct) =>
            {
                var body = await request.ReadFromJsonAsync<ResetPasswordRequest>(ct);
                if (body == null || string.IsNullOrWhiteSpace(body.NewPassword))
                    return Results.BadRequest(new { error = "newPassword is required" });

                var (success, error) = await service.ResetPasswordAsync(username, body.NewPassword, ct);
                if (!success)
                    return Results.NotFound(new { error });

                return Results.Ok(new { reset = true, username });
            }
        );

        app.MapGet(
            "/api/users",
            async (string? search, int? skip, int? take, UserAdminService service, CancellationToken ct) =>
            {
                var (users, total) = await service.ListUsersAsync(search, skip, take, ct);
                return Results.Ok(new { users, total });
            }
        );

        app.MapGet(
            "/api/users/{username}",
            async (string username, UserAdminService service, CancellationToken ct) =>
            {
                var user = await service.GetUserDetailAsync(username, ct);
                if (user == null)
                    return Results.NotFound(new { error = "user not found" });

                return Results.Ok(user);
            }
        );

        app.MapPost(
            "/api/users/{username}/ban",
            async (string username, HttpRequest request, UserAdminService service, CancellationToken ct) =>
            {
                var body = await request.ReadFromJsonAsync<BanRequest>(ct);
                var (success, error, sessionsKicked) = await service.BanUserAsync(username, body?.Reason, ct);
                if (!success)
                    return Results.NotFound(new { error });

                return Results.Ok(
                    new
                    {
                        banned = true,
                        username,
                        sessionsKicked,
                    }
                );
            }
        );

        app.MapPost(
            "/api/users/{username}/unban",
            async (string username, UserAdminService service, CancellationToken ct) =>
            {
                var (success, error) = await service.UnbanUserAsync(username, ct);
                if (!success)
                    return Results.NotFound(new { error });

                return Results.Ok(new { unbanned = true, username });
            }
        );

        app.MapPost(
            "/api/users/{username}/kick",
            async (string username, UserAdminService service, CancellationToken ct) =>
            {
                var (success, error, sessionsClosed) = await service.KickUserAsync(username, ct);
                if (!success)
                    return Results.NotFound(new { error });

                return Results.Ok(
                    new
                    {
                        kicked = true,
                        username,
                        sessionsClosed,
                    }
                );
            }
        );

        app.MapGet(
            "/api/servers/clients",
            (UserAdminService service) =>
            {
                var clients = service.GetConnectedClients();
                return Results.Ok(new { clients, total = clients.Length });
            }
        );

        app.MapGet(
            "/api/stats",
            async (UserAdminService service, CancellationToken ct) =>
            {
                var stats = await service.GetStatsAsync(ct);
                return Results.Ok(stats);
            }
        );

        return app;
    }

    private static async Task<IResult> HandleBroadcastAsync(string? target, HttpRequest request, BroadcastService broadcast, ILoggerFactory loggerFactory)
    {
        var body = await request.ReadFromJsonAsync<BroadcastRequest>();
        if (body == null || string.IsNullOrWhiteSpace(body.Message))
            return Results.BadRequest(new { error = "message is required" });

        ServerType[]? serverTypes = target switch
        {
            null or "" or "all" => [ServerType.Area, ServerType.Msg],
            "area" => [ServerType.Area],
            "msg" => [ServerType.Msg],
            _ => null,
        };

        if (serverTypes == null)
            return Results.BadRequest(new { error = "target must be area, msg, or all" });

        var log = loggerFactory.CreateLogger("API");
        var logPrefix = target is null or "" or "all" ? "API broadcast" : $"API {target} broadcast";
        log.LogInformation("{Prefix}: {Message}", logPrefix, body.Message);

        var result = await broadcast.BroadcastToServersAsync(body.Message, serverTypes, request.HttpContext.RequestAborted);

        if (serverTypes.Length == 2)
            return Results.Ok(
                new
                {
                    sent = true,
                    areaClients = result.AreaClients,
                    msgClients = result.MsgClients,
                }
            );
        if (serverTypes[0] == ServerType.Area)
            return Results.Ok(new { sent = true, areaClients = result.AreaClients });
        return Results.Ok(new { sent = true, msgClients = result.MsgClients });
    }
}
