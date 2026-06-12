using AISpace.Common;
using AISpace.Common.Game;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AISpace.Server;

public sealed record GameServerContext(ILoggerFactory LoggerFactory, IServiceScopeFactory ScopeFactory, PacketDispatcher Dispatcher, SharedState State, GameServerHealthRegistry HealthRegistry, int MaxConcurrentClients, int PacketChannelCapacity, int MaxReceiveFrameSize, int ClientReadTimeoutSeconds, int TickRateHz)
{
    public static GameServerContext Create(IServiceProvider services, int maxConcurrentClients, int packetChannelCapacity, int maxReceiveFrameSize, int clientReadTimeoutSeconds, int tickRateHz) =>
        new(services.GetRequiredService<ILoggerFactory>(), services.GetRequiredService<IServiceScopeFactory>(), services.GetRequiredService<PacketDispatcher>(), services.GetRequiredService<SharedState>(), services.GetRequiredService<GameServerHealthRegistry>(), maxConcurrentClients, packetChannelCapacity, maxReceiveFrameSize, clientReadTimeoutSeconds, tickRateHz);
}
