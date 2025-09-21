using AISpace.Common.DAL;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AISpace.Area.Server;

internal class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Logging.AddConsole();
        builder.Services.AddSingleton<AuthChannel>();
        builder.Services.AddSingleton<AreaChannel>();
        builder.Services.AddSingleton<MsgChannel>();
        builder.Services.Configure<TCPServerConfig>(builder.Configuration.GetSection("TCPServer"));
        //Database
        builder.Services.AddDbContext<MainContext>();

        //Repo
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        //builder.Services.AddScoped<ICharacterRepository, ICharacterRepository>();
        builder.Services.AddScoped<IWorldRepository, WorldRepository>();

        //Add TCP Listeners
        builder.Services.AddHostedService<TcpListenerService<AuthChannel>>();
        builder.Services.AddHostedService<TcpListenerService<AreaChannel>>();
        builder.Services.AddHostedService<TcpListenerService<MsgChannel>>();

        var host = builder.Build();
        await host.RunAsync();


    }
}
