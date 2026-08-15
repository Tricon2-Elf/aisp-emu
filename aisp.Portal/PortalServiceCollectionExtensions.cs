using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace aisp.Portal;

public static class PortalServiceCollectionExtensions
{
    public static IServiceCollection AddPortalBackendClients(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<PortalBackendOptions>()
            .Bind(configuration.GetSection(PortalBackendOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ServiceToken),
                "PortalBackend:ServiceToken must be configured when a portal is enabled."
            )
            .ValidateOnStart();
        services.AddTransient<PortalServiceTokenHandler>();

        foreach (var name in new[] { PortalHttpClientNames.Auth, PortalHttpClientNames.Msg, PortalHttpClientNames.Area })
        {
            services.AddHttpClient(name, (sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptions<PortalBackendOptions>>().Value;
                    client.BaseAddress = new Uri(options.ApiBaseUrl.TrimEnd('/') + "/");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                })
                .AddHttpMessageHandler<PortalServiceTokenHandler>()
                .AddAsKeyed();
        }

        services.AddScoped<AuthPortalApiClient>();
        services.AddScoped<AreaPortalApiClient>();
        services.AddScoped<MsgPortalApiClient>();
        return services;
    }
}

public sealed class PortalServiceTokenHandler(IOptions<PortalBackendOptions> options) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        request.Headers.Add("X-Portal-Service-Token", options.Value.ServiceToken);
        return base.SendAsync(request, ct);
    }
}
