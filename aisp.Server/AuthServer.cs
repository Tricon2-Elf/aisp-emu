using aisp.Common;
using aisp.Common.Game;

namespace aisp.Server;

public class AuthServer(ILogger<AuthServer> logger, GameServerContext ctx, int port)
    : GameServerBase<AuthServer>(logger, ctx, port)
{
    protected override ServerType ActiveServerType => ServerType.Auth;
}
