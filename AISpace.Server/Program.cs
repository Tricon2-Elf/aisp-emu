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
using Microsoft.Extensions.DependencyInjection;
using NLog.Extensions.Logging;

namespace AISpace.Server;

internal class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
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

        builder.Services.AddSingleton<SharedState>();
        // Add all IPacketHandler classsess
        builder.Services.Scan(scan => scan.FromAssemblyOf<IPacketHandler>().AddClasses(classes => classes.AssignableTo<IPacketHandler>()).AsImplementedInterfaces().WithScopedLifetime());

        builder.Services.AddSingleton<PacketDispatcher>();

        builder.Services.AddSingleton<AuthChannel>(_ => new(Channel.CreateUnbounded<Packet>()));
        builder.Services.AddSingleton<IHostedService>(sp => new TcpListenerService(sp.GetRequiredService<ILogger<TcpListenerService>>(), sp.GetRequiredService<AuthChannel>().Channel, "Auth", 50050, sp.GetRequiredService<ILoggerFactory>(), sp.GetRequiredService<SharedState>()));
        builder.Services.AddHostedService<AuthServer>();

        builder.Services.AddSingleton<MsgChannel>(_ => new(Channel.CreateUnbounded<Packet>()));
        builder.Services.AddSingleton<IHostedService>(sp => new TcpListenerService(sp.GetRequiredService<ILogger<TcpListenerService>>(), sp.GetRequiredService<MsgChannel>().Channel, "Msg", 50052, sp.GetRequiredService<ILoggerFactory>(), sp.GetRequiredService<SharedState>()));

        builder.Services.AddHostedService<MsgServer>();

        builder.Services.AddSingleton<AreaChannel>(_ => new(Channel.CreateUnbounded<Packet>()));
        builder.Services.AddSingleton<IHostedService>(sp => new TcpListenerService(sp.GetRequiredService<ILogger<TcpListenerService>>(), sp.GetRequiredService<AreaChannel>().Channel, "Area", 50054, sp.GetRequiredService<ILoggerFactory>(), sp.GetRequiredService<SharedState>()));
        builder.Services.AddHostedService<AreaServer>();

        var host = builder.Build();

        // Ensure database and Maps table exist, then seed maps if empty
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainContext>();
            var serverOptions = scope.ServiceProvider.GetRequiredService<IOptions<ServerOptions>>().Value;
            await db.Database.EnsureCreatedAsync();
            await MapRepository.SeedMapsIfEmptyAsync(db);
            await MapLinkRepository.SeedMapLinksIfEmptyAsync(db);
            await WorldRepository.SeedWorldsIfEmptyAsync(db, serverOptions.IPOverride);
            await ChannelRepository.SeedChannelsIfEmptyAsync(db, serverOptions.IPOverride, areaPort: 50054);
        }

        await host.RunAsync();
    }
}
