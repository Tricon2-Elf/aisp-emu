using AISpace.Common;
using AISpace.Common.DAL;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AISpace.Server;

public sealed record GameServerContext(
    ILoggerFactory LoggerFactory,
    MainContext Db,
    PacketDispatcher Dispatcher,
    SharedState State,
    GameServerHealthRegistry HealthRegistry,
    IUserRepository UserRepo,
    IWorldRepository WorldRepo,
    int MaxConcurrentClients,
    int PacketChannelCapacity)
{
    public static GameServerContext Create(IServiceProvider services, int maxConcurrentClients, int packetChannelCapacity) =>
        new(
            services.GetRequiredService<ILoggerFactory>(),
            services.GetRequiredService<MainContext>(),
            services.GetRequiredService<PacketDispatcher>(),
            services.GetRequiredService<SharedState>(),
            services.GetRequiredService<GameServerHealthRegistry>(),
            services.GetRequiredService<IUserRepository>(),
            services.GetRequiredService<IWorldRepository>(),
            maxConcurrentClients,
            packetChannelCapacity);
}
