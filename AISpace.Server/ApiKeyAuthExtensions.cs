using AISpace.Common.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AISpace.Server;

internal static class ApiKeyAuthExtensions
{
    internal static WebApplication UseApiKeyAuthForApiRoutes(this WebApplication app)
    {
        app.Use(
            async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    var apiSettings = context
                        .RequestServices.GetRequiredService<IOptions<ApiSettings>>()
                        .Value;
                    if (string.IsNullOrEmpty(apiSettings.ApiKey))
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsJsonAsync(
                            new { error = "API key not configured" }
                        );
                        return;
                    }

                    string? providedKey = context.Request.Headers["X-Api-Key"];
                    if (providedKey != apiSettings.ApiKey)
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
                        return;
                    }
                }
                await next();
            }
        );

        return app;
    }
}
