global using System.Threading.Channels;
global using AISpace.Common.Config;
global using AISpace.Common.DAL;
global using AISpace.Common.DAL.Repositories;
global using AISpace.Common.Network;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
using System.Text;
using AISpace.Common.Game;
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
        //Database
        builder.Services.AddDbContext<MainContext>();

        //Repo
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IWorldRepository, WorldRepository>();
        builder.Services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        builder.Services.AddScoped<ICharacterRepository, CharacterRepository>();
        builder.Services.AddScoped<ICharacterRepository, CharacterRepository>();

        builder.Services.AddSingleton<SharedState>();
        // Add all IPacketHandler classsess
        builder.Services.Scan(scan => scan
            .FromAssemblyOf<IPacketHandler>()
            .AddClasses(classes => classes.AssignableTo<IPacketHandler>())
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        builder.Services.AddSingleton<PacketDispatcher>();

        builder.Services.AddSingleton<AuthChannel>(_ => new(Channel.CreateUnbounded<Packet>()));
        builder.Services.AddSingleton<IHostedService>(sp =>
            new TcpListenerService(
                sp.GetRequiredService<ILogger<TcpListenerService>>(),
                sp.GetRequiredService<AuthChannel>().Channel,
                "Auth",
                50050, sp.GetRequiredService<ILoggerFactory>()));
        builder.Services.AddHostedService<AuthServer>();

        builder.Services.AddSingleton<MsgChannel>(_ => new(Channel.CreateUnbounded<Packet>()));
        builder.Services.AddSingleton<IHostedService>(sp =>
        new TcpListenerService(
            sp.GetRequiredService<ILogger<TcpListenerService>>(),
            sp.GetRequiredService<MsgChannel>().Channel,
            "Msg",
            50052, sp.GetRequiredService<ILoggerFactory>()));

        builder.Services.AddHostedService<MsgServer>();

        builder.Services.AddSingleton<AreaChannel>(_ => new(Channel.CreateUnbounded<Packet>()));
        builder.Services.AddSingleton<IHostedService>(sp =>
            new TcpListenerService(
                sp.GetRequiredService<ILogger<TcpListenerService>>(),
                sp.GetRequiredService<AreaChannel>().Channel,
                "Area",
                50054, sp.GetRequiredService<ILoggerFactory>()));
        builder.Services.AddHostedService<AreaServer>();



        var host = builder.Build();
        foreach (var svc in builder.Services)
        {
            var type = svc.ServiceType.ToString();
            if (type.StartsWith("AI") && !type.Contains("IPacketHandler"))
                Console.WriteLine($"Registered: {svc.ServiceType}");
        }
        await host.RunAsync();
    }
}
