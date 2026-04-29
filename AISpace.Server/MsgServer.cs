using AISpace.Common;
using AISpace.Common.Game;

namespace AISpace.Server;

public class MsgServer(ILogger<MsgServer> logger, MainContext db, IUserRepository userRepo, int port, ILoggerFactory loggerFactory, IWorldRepository worldRepo, PacketDispatcher dispatcher, SharedState state, GameServerHealthRegistry healthRegistry)
    : GameServerBase<MsgServer>(logger, db, userRepo, port, "Msg", loggerFactory, worldRepo, dispatcher, state, healthRegistry, GameServerHealthRegistry.Keys.MsgServer)
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
