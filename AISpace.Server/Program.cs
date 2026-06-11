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
using AISpace.Common.Services;
using AISpace.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Extensions.Logging;

namespace AISpace.Server;

internal class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        // Sdk.Web auto-binds Kestrel:Endpoints from appsettings; FrameworkReference-only projects do not.
        builder.WebHost.ConfigureKestrel((context, serverOptions) => serverOptions.Configure(context.Configuration.GetSection("Kestrel")));
        // IP override: set Server__IPOverride (e.g. Server__IPOverride=host.docker.internal) or IP_OVERRIDE env to replace localhost addresses in Docker.
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("IP_OVERRIDE")))
            builder.Configuration["Server:IPOverride"] = Environment.GetEnvironmentVariable("IP_OVERRIDE");

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
        builder.Logging.AddNLog();

        builder.Services.Configure<ServerOptions>(builder.Configuration.GetSection("Server"));
        builder.Services.AddDbContext<MainContext>((sp, options) => sp.GetRequiredService<IOptions<ServerOptions>>().Value.DbOptions.ConfigureDbContext(options));
        builder.Services.AddDbContextFactory<MainContext>((sp, options) => sp.GetRequiredService<IOptions<ServerOptions>>().Value.DbOptions.ConfigureDbContext(options));

        //Repo
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IWorldRepository, WorldRepository>();
        builder.Services.AddScoped<IChannelRepository, ChannelRepository>();
        builder.Services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        builder.Services.AddScoped<ICharacterRepository, CharacterRepository>();
        builder.Services.AddScoped<IMapRepository, MapRepository>();
        builder.Services.AddScoped<IMapLinkRepository, MapLinkRepository>();
        builder.Services.AddScoped<IItemRepository, ItemRepository>();
        builder.Services.AddSingleton<ISessionPresenceRepository, SessionPresenceRepository>();
        builder.Services.AddSingleton<IPendingMapTransferRepository, PendingMapTransferRepository>();
        builder.Services.AddScoped<DirectMapLinkTransitionService>();
        builder.Services.AddSingleton<IItemBaseListCache, ItemBaseListCache>();

        builder.Services.AddSingleton<SharedState>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ServerOptions>>().Value;
            var sessionStore = new SessionStore();
            var sessionClientRegistry = new SessionClientRegistry();
            var pendingTransitionStore = new PendingTransitionStore();

            if (options.UseDistributedSessionPresence)
            {
                return new SharedState(sessionStore, sessionClientRegistry, pendingTransitionStore, sp.GetRequiredService<ISessionPresenceRepository>(), sp.GetRequiredService<IPendingMapTransferRepository>());
            }

            return new SharedState(sessionStore, sessionClientRegistry, pendingTransitionStore);
        });
        // Add all IPacketHandler classsess
        builder.Services.Scan(scan => scan.FromAssemblyOf<IPacketHandler>().AddClasses(classes => classes.AssignableTo<IPacketHandler>()).AsImplementedInterfaces().WithScopedLifetime());

        builder.Services.AddSingleton<PacketDispatcher>();
        builder.Services.Configure<MaintenanceOptions>(builder.Configuration.GetSection("Maintenance"));
        builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));
        builder.Services.AddSingleton<BroadcastService>();
        builder.Services.AddScoped<UserAdminService>();
        builder.Services.AddSingleton<GameServerHealthRegistry>();
        builder.Services.AddHealthChecks();

        // Read per-server config from the Server section
        var authEnabled = builder.Configuration.GetValue("Server:AuthServer:Enabled", true);
        var authPort = builder.Configuration.GetValue("Server:AuthServer:Port", 50050);
        var msgEnabled = builder.Configuration.GetValue("Server:MsgServer:Enabled", true);
        var msgPort = builder.Configuration.GetValue("Server:MsgServer:Port", 50052);
        var areaEnabled = builder.Configuration.GetValue("Server:AreaServer:Enabled", true);
        var areaPort = builder.Configuration.GetValue("Server:AreaServer:Port", 50054);

        GameServerContext BuildGameServerContext(IServiceProvider sp)
        {
            var o = sp.GetRequiredService<IOptions<ServerOptions>>().Value;
            return GameServerContext.Create(sp, o.MaxConcurrentClients, o.PacketChannelCapacity, o.MaxReceiveFrameSize, o.TickRateHz);
        }

        if (authEnabled)
            builder.Services.AddHostedService(sp => ActivatorUtilities.CreateInstance<AuthServer>(sp, BuildGameServerContext(sp), authPort));

        if (msgEnabled)
            builder.Services.AddHostedService(sp => ActivatorUtilities.CreateInstance<MsgServer>(sp, BuildGameServerContext(sp), msgPort));

        if (areaEnabled)
            builder.Services.AddHostedService(sp => ActivatorUtilities.CreateInstance<AreaServer>(sp, BuildGameServerContext(sp), areaPort));

        builder.Services.AddHostedService<ScheduledMaintenanceService>();

        var app = builder.Build();

        app.UseApiKeyAuthForApiRoutes();
        app.MapAispaceHttpEndpoints();

        // Ensure database and Maps table exist, then seed maps if empty
        using (var scope = app.Services.CreateScope())
        {
            var serverOptions = scope.ServiceProvider.GetRequiredService<IOptions<ServerOptions>>().Value;
            serverOptions.DbOptions.EnsureDataDirectoryExists();
            var db = scope.ServiceProvider.GetRequiredService<MainContext>();
            await db.Database.MigrateAsync();
            var sessionRepo = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
            await sessionRepo.InvalidateExpiredAsync();
            var seedDir = Path.Combine(AppContext.BaseDirectory, "seedData");
            await MapRepository.SeedMapsIfEmptyAsync(db, Path.Combine(seedDir, "maps.json"));
            await MapRepository.EnsureSeedMapsPresentAsync(db, Path.Combine(seedDir, "maps.json"));
            await MapLinkRepository.SeedMapLinksIfEmptyAsync(db, Path.Combine(seedDir, "mapLinks.json"));
            await WorldRepository.SeedWorldsIfEmptyAsync(db, Path.Combine(seedDir, "worlds.json"), serverOptions.IPOverride, (ushort)serverOptions.MsgServer.Port);
            await ChannelRepository.SeedChannelsIfEmptyAsync(db, Path.Combine(seedDir, "channels.json"), serverOptions.IPOverride, areaPort: (ushort)serverOptions.AreaServer.Port);
            await ItemRepository.SeedItemsIfEmptyAsync(db, Path.Combine(seedDir, "baseItems.json"));
            await scope.ServiceProvider.GetRequiredService<IItemBaseListCache>().WarmAsync();
        }

        await app.RunAsync();
    }
}
