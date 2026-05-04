global using System.Threading.Channels;
global using AISpace.Common.Config;
global using AISpace.Common.DAL;
global using AISpace.Common.DAL.Repositories;
global using AISpace.Network;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
using System.Text;
using AISpace.Common;
using AISpace.Common.Game;
using AISpace.Common.Handlers.Area;
using AISpace.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog.Extensions.Logging;

namespace AISpace.Server;

internal class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        // IP override: set Server__IPOverride (e.g. Server__IPOverride=host.docker.internal) or IP_OVERRIDE env to replace localhost addresses in Docker.
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("IP_OVERRIDE")))
            builder.Configuration["Server:IPOverride"] = Environment.GetEnvironmentVariable("IP_OVERRIDE");

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Logging.AddNLog();

        builder.Services.Configure<ServerOptions>(builder.Configuration.GetSection("Server"));
        //Database
        builder.Services.AddDbContext<MainContext>();
        builder.Services.AddDbContextFactory<MainContext>();

        //Repo
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IWorldRepository, WorldRepository>();
        builder.Services.AddScoped<IChannelRepository, ChannelRepository>();
        builder.Services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        builder.Services.AddScoped<ICharacterRepository, CharacterRepository>();
        builder.Services.AddScoped<IMapRepository, MapRepository>();
        builder.Services.AddScoped<IMapLinkRepository, MapLinkRepository>();
        builder.Services.AddSingleton<ISessionPresenceRepository, SessionPresenceRepository>();
        builder.Services.AddSingleton<IPendingMapTransferRepository, PendingMapTransferRepository>();
        builder.Services.AddScoped<DirectMapLinkTransitionService>();

        builder.Services.AddSingleton<SharedState>(sp => new SharedState(new SessionStore(), new SessionClientRegistry(), new PendingTransitionStore(), sp.GetRequiredService<ISessionPresenceRepository>(), sp.GetRequiredService<IPendingMapTransferRepository>()));
        // Add all IPacketHandler classsess
        builder.Services.Scan(scan => scan.FromAssemblyOf<IPacketHandler>().AddClasses(classes => classes.AssignableTo<IPacketHandler>()).AsImplementedInterfaces().WithScopedLifetime());

        builder.Services.AddSingleton<PacketDispatcher>();
        builder.Services.Configure<MaintenanceOptions>(builder.Configuration.GetSection("Maintenance"));
        builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));
        builder.Services.AddSingleton<BroadcastService>();
        builder.Services.AddSingleton<GameServerHealthRegistry>();
        builder.Services.AddHealthChecks();

        builder.Services.AddHostedService(sp => new AuthServer(
            sp.GetRequiredService<ILogger<AuthServer>>(),
            sp.GetRequiredService<MainContext>(),
            sp.GetRequiredService<IUserRepository>(),
            50050,
            sp.GetRequiredService<ILoggerFactory>(),
            sp.GetRequiredService<IWorldRepository>(),
            sp.GetRequiredService<PacketDispatcher>(),
            sp.GetRequiredService<SharedState>(),
            sp.GetRequiredService<GameServerHealthRegistry>()
        ));

        builder.Services.AddHostedService(sp => new MsgServer(
            sp.GetRequiredService<ILogger<MsgServer>>(),
            sp.GetRequiredService<MainContext>(),
            sp.GetRequiredService<IUserRepository>(),
            50052,
            sp.GetRequiredService<ILoggerFactory>(),
            sp.GetRequiredService<IWorldRepository>(),
            sp.GetRequiredService<PacketDispatcher>(),
            sp.GetRequiredService<SharedState>(),
            sp.GetRequiredService<GameServerHealthRegistry>()
        ));

        builder.Services.AddHostedService<ScheduledMaintenanceService>();

        builder.Services.AddHostedService(sp => new AreaServer(
            sp.GetRequiredService<ILogger<AreaServer>>(),
            sp.GetRequiredService<MainContext>(),
            sp.GetRequiredService<IUserRepository>(),
            50054,
            sp.GetRequiredService<ILoggerFactory>(),
            sp.GetRequiredService<IWorldRepository>(),
            sp.GetRequiredService<PacketDispatcher>(),
            sp.GetRequiredService<SharedState>(),
            sp.GetRequiredService<GameServerHealthRegistry>()
        ));

        var app = builder.Build();

        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                var apiSettings = context.RequestServices.GetRequiredService<IOptions<ApiSettings>>().Value;
                if (string.IsNullOrEmpty(apiSettings.ApiKey))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { error = "API key not configured" });
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
        });

        app.MapHealthChecks("/health");
        app.MapGet(
            "/healthz",
            (GameServerHealthRegistry registry, SharedState state) =>
            {
                var servers = registry.GetSnapshot(state);
                var allHealthy = servers.Values.All(s => s.State == "healthy");
                return Results.Json(new { status = allHealthy ? "Healthy" : "Unhealthy", servers }, statusCode: allHealthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
            }
        );

        app.MapPost(
            "/api/broadcast",
            async (HttpRequest request, BroadcastService broadcast, ILoggerFactory loggerFactory) =>
            {
                var body = await request.ReadFromJsonAsync<BroadcastRequest>();
                if (body == null || string.IsNullOrWhiteSpace(body.Message))
                    return Results.BadRequest(new { error = "message is required" });

                var log = loggerFactory.CreateLogger("API");
                log.LogInformation("API broadcast: {Message}", body.Message);

                var result = await broadcast.BroadcastAsync(body.Message, request.HttpContext.RequestAborted);
                return Results.Ok(new { sent = true, areaClients = result.AreaClients, msgClients = result.MsgClients });
            }
        );

        // Ensure database and Maps table exist, then seed maps if empty
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainContext>();
            var serverOptions = scope.ServiceProvider.GetRequiredService<IOptions<ServerOptions>>().Value;
            await db.Database.MigrateAsync();
            await MapRepository.SeedMapsIfEmptyAsync(db);
            await MapRepository.EnsureSeedMapsPresentAsync(db);
            await MapLinkRepository.SeedMapLinksIfEmptyAsync(db);
            await MapLinkRepository.NormalizeSeedMapLinksAsync(db);
            await WorldRepository.SeedWorldsIfEmptyAsync(db, serverOptions.IPOverride);
            await ChannelRepository.SeedChannelsIfEmptyAsync(db, serverOptions.IPOverride, areaPort: 50054);
        }

        await app.RunAsync();
    }
}
