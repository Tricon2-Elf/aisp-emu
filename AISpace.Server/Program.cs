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
using Microsoft.Extensions.Configuration;
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

        // Read per-server config from the Server section
        var authEnabled = builder.Configuration.GetValue("Server:AuthServer:Enabled", true);
        var authPort = builder.Configuration.GetValue("Server:AuthServer:Port", 50050);
        var msgEnabled = builder.Configuration.GetValue("Server:MsgServer:Enabled", true);
        var msgPort = builder.Configuration.GetValue("Server:MsgServer:Port", 50052);
        var areaEnabled = builder.Configuration.GetValue("Server:AreaServer:Enabled", true);
        var areaPort = builder.Configuration.GetValue("Server:AreaServer:Port", 50054);

        if (authEnabled)
            builder.Services.AddHostedService(sp => ActivatorUtilities.CreateInstance<AuthServer>(sp, authPort));

        if (msgEnabled)
            builder.Services.AddHostedService(sp => ActivatorUtilities.CreateInstance<MsgServer>(sp, msgPort));

        if (areaEnabled)
            builder.Services.AddHostedService(sp => ActivatorUtilities.CreateInstance<AreaServer>(sp, areaPort));

        builder.Services.AddHostedService<ScheduledMaintenanceService>();

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
            (GameServerHealthRegistry registry, ISessionPresenceRepository presenceRepo) =>
            {
                var servers = registry.GetSnapshot(presenceRepo);
                var allHealthy = servers.Values.All(s => s.State == "healthy");
                return Results.Json(new { status = allHealthy ? "Healthy" : "Unhealthy", servers }, statusCode: allHealthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
            }
        );

        async Task<IResult> HandleBroadcastAsync(string? target, HttpRequest request, BroadcastService broadcast, ILoggerFactory loggerFactory)
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
                return Results.Ok(new { sent = true, areaClients = result.AreaClients, msgClients = result.MsgClients });
            if (serverTypes[0] == ServerType.Area)
                return Results.Ok(new { sent = true, areaClients = result.AreaClients });
            return Results.Ok(new { sent = true, msgClients = result.MsgClients });
        }

        app.MapPost("/api/broadcast", (HttpRequest request, BroadcastService broadcast, ILoggerFactory loggerFactory) => HandleBroadcastAsync(null, request, broadcast, loggerFactory));
        app.MapPost("/api/area/broadcast", (HttpRequest request, BroadcastService broadcast, ILoggerFactory loggerFactory) => HandleBroadcastAsync("area", request, broadcast, loggerFactory));
        app.MapPost("/api/msg/broadcast", (HttpRequest request, BroadcastService broadcast, ILoggerFactory loggerFactory) => HandleBroadcastAsync("msg", request, broadcast, loggerFactory));

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
            await WorldRepository.SeedWorldsIfEmptyAsync(db, serverOptions.IPOverride, (ushort)serverOptions.MsgServer.Port);
            await ChannelRepository.SeedChannelsIfEmptyAsync(db, serverOptions.IPOverride, areaPort: (ushort)serverOptions.AreaServer.Port);
        }

        await app.RunAsync();
    }
}
