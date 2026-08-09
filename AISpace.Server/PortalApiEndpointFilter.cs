using System.Security.Cryptography;
using System.Text;
using AISpace.Portal.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AISpace.Server;

internal sealed class PortalApiEndpointFilter(
    IOptions<PortalBackendOptions> options,
    ILogger<PortalApiEndpointFilter> logger
) : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var configuredToken = options.Value.ServiceToken;
        var suppliedToken = context.HttpContext.Request.Headers["X-Portal-Service-Token"].ToString();
        if (string.IsNullOrWhiteSpace(configuredToken) || string.IsNullOrWhiteSpace(suppliedToken))
        {
            logger.LogWarning("Rejected portal API request to {Path}: portal service token is missing", context.HttpContext.Request.Path);
            return ValueTask.FromResult<object?>(TypedResults.Unauthorized());
        }

        var expected = Encoding.UTF8.GetBytes(configuredToken);
        var actual = Encoding.UTF8.GetBytes(suppliedToken);
        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            logger.LogWarning("Rejected portal API request to {Path}: portal service token is invalid", context.HttpContext.Request.Path);
            return ValueTask.FromResult<object?>(TypedResults.Unauthorized());
        }

        return next(context);
    }
}
