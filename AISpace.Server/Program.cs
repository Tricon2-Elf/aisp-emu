global using System.Threading.Channels;
global using AISpace.Common.Config;
global using AISpace.Common.DAL;
global using AISpace.Common.DAL.Repositories;
global using AISpace.Common.Network;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
using System.Text;
using AISpace.Common.Game;
using AISpace.Common.Network.Handlers;
using AISpace.Common.Network.Packets;
using Microsoft.Extensions.DependencyInjection;
using NLog.Extensions.Logging;

namespace AISpace.Server;

internal class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Logging.AddNLog();

        builder.Services.Configure<ServerOptions>(builder.Configuration.GetSection("Server"));
        builder.Services.AddDbContext<MainContext>();

        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IWorldRepository, WorldRepository>();
        builder.Services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        builder.Services.AddScoped<ICharacterRepository, CharacterRepository>();

        // SharedState - ОБЯЗАТЕЛЬНО Singleton
        builder.Services.AddSingleton<SharedState>();

        builder.Services.Scan(scan => scan
            .FromAssemblyOf<IPacketHandler>()
            .AddClasses(classes => classes.AssignableTo<IPacketHandler>())
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        builder.Services.AddSingleton<PacketDispatcher>();

        // Auth
        builder.Services.AddSingleton<AuthChannel>(_ => new(Channel.CreateUnbounded<Packet>()));
        builder.Services.AddSingleton<IHostedService>(sp =>
            new TcpListenerService(
                sp.GetRequiredService<ILogger<TcpListenerService>>(),
                sp.GetRequiredService<AuthChannel>().Channel,
                "Auth", 50050, sp.GetRequiredService<ILoggerFactory>(),
                sp.GetRequiredService<SharedState>())); // Передаем SharedState
        builder.Services.AddHostedService<AuthServer>();

        // Msg
        builder.Services.AddSingleton<MsgChannel>(_ => new(Channel.CreateUnbounded<Packet>()));
        builder.Services.AddSingleton<IHostedService>(sp =>
            new TcpListenerService(
                sp.GetRequiredService<ILogger<TcpListenerService>>(),
                sp.GetRequiredService<MsgChannel>().Channel,
                "Msg", 50052, sp.GetRequiredService<ILoggerFactory>(),
                sp.GetRequiredService<SharedState>())); // Передаем SharedState
        builder.Services.AddHostedService<MsgServer>();

        // Area
        builder.Services.AddSingleton<AreaChannel>(_ => new(Channel.CreateUnbounded<Packet>()));
        builder.Services.AddSingleton<IHostedService>(sp =>
            new TcpListenerService(
                sp.GetRequiredService<ILogger<TcpListenerService>>(),
                sp.GetRequiredService<AreaChannel>().Channel,
                "Area", 50054, sp.GetRequiredService<ILoggerFactory>(),
                sp.GetRequiredService<SharedState>())); // Передаем SharedState
        builder.Services.AddHostedService<AreaServer>();

        var host = builder.Build();
        await host.RunAsync();
    }
}