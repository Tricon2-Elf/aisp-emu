using aisp.Common;
using aisp.Common.Game;

namespace aisp.Server;

public class AreaServer(ILogger<AreaServer> logger, GameServerContext ctx, int port)
    : GameServerBase<AreaServer>(logger, ctx, port)
{
    protected override ServerType ActiveServerType => ServerType.Area;
}
