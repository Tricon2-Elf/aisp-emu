using AISpace.Common;
using AISpace.Common.Config;
using AISpace.Common.Game;
using Microsoft.Extensions.Options;

namespace AISpace.Server;

public class MsgServer(ILogger<MsgServer> logger, MainContext db, IUserRepository userRepo, int port, ILoggerFactory loggerFactory, IWorldRepository worldRepo, PacketDispatcher dispatcher, SharedState state, GameServerHealthRegistry healthRegistry, IOptions<ServerOptions> serverOptions)
    : GameServerBase<MsgServer>(logger, db, userRepo, port, "Msg", loggerFactory, worldRepo, dispatcher, state, healthRegistry, serverOptions.Value.MsgServer.MaxConcurrentClients, ServerOptions.NormalizePacketChannelCapacity(serverOptions.Value.PacketChannelCapacity), GameServerHealthRegistry.Keys.MsgServer)
{
    protected override ServerType ActiveServerType => ServerType.Msg;

    protected override void OnTick(CancellationToken ct)
    {
        while (State.TryDequeueMessage(out var message))
        {
            Logger.LogInformation("{id} sent {message}", message.id, message.message);
            // Send message to all other users
        }
    }
}
