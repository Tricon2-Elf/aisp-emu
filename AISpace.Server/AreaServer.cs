using AISpace.Common;
using AISpace.Common.Game;

namespace AISpace.Server;

public class AreaServer(ILogger<AreaServer> logger, GameServerContext ctx, int port) : GameServerBase<AreaServer>(logger, ctx, port)
{
    protected override ServerType ActiveServerType => ServerType.Area;
}
