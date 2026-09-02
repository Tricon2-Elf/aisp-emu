using aisp.Common;
using aisp.Common.Game;
using aisp.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace aisp.Server;

public sealed record GameServerContext(
    ILoggerFactory LoggerFactory,
    IServiceScopeFactory ScopeFactory,
    PacketDispatcher Dispatcher,
    SharedState State,
    GameServerHealthRegistry HealthRegistry,
    int MaxConcurrentClients,
    int PacketChannelCapacity,
    int MaxReceiveFrameSize,
    int ClientReadTimeoutSeconds,
    int ClientSendTimeoutSeconds,
    TcpSocketOptions TcpSocketOptions
)
{
    public static GameServerContext Create(
        IServiceProvider services,
        int maxConcurrentClients,
        int packetChannelCapacity,
        int maxReceiveFrameSize,
        int clientReadTimeoutSeconds,
        int clientSendTimeoutSeconds,
        TcpSocketOptions? tcpSocketOptions = null
    ) =>
        new(
            services.GetRequiredService<ILoggerFactory>(),
            services.GetRequiredService<IServiceScopeFactory>(),
            services.GetRequiredService<PacketDispatcher>(),
            services.GetRequiredService<SharedState>(),
            services.GetRequiredService<GameServerHealthRegistry>(),
            maxConcurrentClients,
            packetChannelCapacity,
            maxReceiveFrameSize,
            clientReadTimeoutSeconds,
            clientSendTimeoutSeconds,
            tcpSocketOptions ?? TcpSocketOptions.Default
        );
}
