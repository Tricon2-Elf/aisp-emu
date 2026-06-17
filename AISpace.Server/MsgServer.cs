using AISpace.Common;
using AISpace.Common.Game;

namespace AISpace.Server;

public class MsgServer(ILogger<MsgServer> logger, GameServerContext ctx, int port) : GameServerBase<MsgServer>(logger, ctx, port)
{
    protected override ServerType ActiveServerType => ServerType.Msg;

    protected override TimeSpan? GameLoopInterval => TickRate;

    protected override void OnTick(CancellationToken ct)
    {
        while (State.TryDequeueMessage(out var message))
        {
            Logger.LogInformation("{id} sent {message}", message.id, message.message);
            // Send message to all other users
        }
    }
}
