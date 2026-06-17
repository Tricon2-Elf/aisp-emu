using AISpace.Common;
using AISpace.Common.Game;

namespace AISpace.Server;

public class MsgServer(ILogger<MsgServer> logger, GameServerContext ctx, int port) : GameServerBase<MsgServer>(logger, ctx, port)
{
    protected override ServerType ActiveServerType => ServerType.Msg;

    protected override IEnumerable<Task> GetAdditionalLoops(CancellationToken ct) => [RunMessageLoop(ct)];

    private async Task RunMessageLoop(CancellationToken ct)
    {
        try
        {
            await foreach (var (id, message) in State.Messages.ReadAllAsync(ct))
                Logger.LogInformation("{id} sent {message}", id, message);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // expected during listener restart
        }
    }
}
